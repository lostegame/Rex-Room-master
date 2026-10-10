namespace Rex_Room_20230414.API;

using System;
using System.Collections.Generic;
using System.Linq;
using Rex_Room_20230414.Database;

// Server-side player progression + the level-up gift drop.
//
// The 2023 client reads progression from GET /api/players/v2/progression/bulk and
// learns about a pending gift from GET /api/gamerewards/v1/pending. We keep Level/XP
// on the Account row (persisted via DatabaseManager.SavePlayerData) and hand out one
// random owned-catalog avatar item every time the level goes up, so a fresh player who
// keeps playing always has something new to wear.
public static class ProgressionHelper
{
    // XP needed to reach the NEXT level. Gentle curve so early levels come fast.
    public static int XpForLevel(int level) => 100 + (level - 1) * 50;

    public static int LevelForXp(int xp)
    {
        int level = 1;
        while (xp >= XpForLevel(level))
        {
            xp -= XpForLevel(level);
            level++;
            if (level > 999) break;
        }
        return level;
    }

    // Grants XP, persists the new level, and enqueues one gift per level gained.
    // Returns the number of levels gained.
    public static int AddXp(Account acc, int amount)
    {
        if (acc == null || amount <= 0) return 0;

        int before = acc.Level <= 0 ? 1 : acc.Level;
        acc.Experience = (acc.Experience <= 0 ? 0 : acc.Experience) + amount;

        int after = LevelForXp(acc.Experience);
        // Keep the remainder as progress toward the next level.
        int consumed = 0;
        for (int l = before; l < after; l++) consumed += XpForLevel(l);
        acc.Experience -= consumed;
        acc.Level = after;

        Rex_Room_20230414.Database.DatabaseManager.SavePlayerData(acc);

        int gained = after - before;
        for (int i = 0; i < gained; i++)
            GiftDrop.EnqueueFor(acc.Id, $"Level {after} Reward");
        return gained;
    }

    public static int GetLevel(Account acc) => acc == null ? 1 : (acc.Level <= 0 ? 1 : acc.Level);
    public static int GetXp(Account acc) => acc == null ? 0 : (acc.Experience <= 0 ? 0 : acc.Experience);
}

// One pending gift per account, stored in memory and consumed by
// GET /api/gamerewards/v1/pending exactly like the reference's reward queue.
//
// A pending reward is either an avatar-item gift (RewardType 1) or a token box
// (RewardType 2). The token box is what the Dorm entry reward uses: the tokens
// are credited when the client collects the pending reward, so they arrive as a
// box the player opens rather than as a silent balance deposit.
public static class GiftDrop
{
    // Reward kinds the client can render.
    public const int RewardTypeAvatarItem = 1;
    public const int RewardTypeTokens = 2;
    public const int RewardTypeConsumable = 3;

    private sealed class Pending
    {
        public string Name = "";
        public int AvatarItemId;
        public int RewardType = RewardTypeAvatarItem;
        public int Tokens;
        public string ConsumableId = "";
        public string CreatedAt = "";
    }

    private static readonly Dictionary<int, List<Pending>> _queue = new();
    private static readonly object _lock = new();
    private static readonly Random _rng = new();

    public static void EnqueueFor(int accountId, string label)
    {
        int itemId = PickRandomOwnedItemId();
        if (itemId <= 0) return;

        // Persist the unlock on the account so the wardrobe keeps showing the
        // gift after the pending queue is drained and after a restart. The queue
        // alone is in-memory and is emptied by /api/gamerewards/v1/pending, which
        // used to make a level-up gift vanish from the wardrobe immediately.
        try
        {
            var acc = AccountHelper.GetAccountById(accountId);
            if (acc != null)
            {
                if (acc.UnlockedAvatarItemIds == null)
                    acc.UnlockedAvatarItemIds = new List<int>();
                if (!acc.UnlockedAvatarItemIds.Contains(itemId))
                {
                    acc.UnlockedAvatarItemIds.Add(itemId);
                    AccountHelper.SaveAccount(acc);
                }
            }
        }
        catch { }

        lock (_lock)
        {
            if (!_queue.TryGetValue(accountId, out var list))
            {
                list = new List<Pending>();
                _queue[accountId] = list;
            }
            list.Add(new Pending
            {
                Name = label,
                AvatarItemId = itemId,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        }
    }

    // A token box: nothing is credited up front. The balance only moves when the
    // client collects the pending reward, which is what makes it read as a gift
    // box the player opens instead of a deposit made behind their back.
    public static bool EnqueueTokens(int accountId, int tokens, string label)
    {
        if (accountId <= 0 || tokens <= 0) return false;

        lock (_lock)
        {
            if (!_queue.TryGetValue(accountId, out var list))
            {
                list = new List<Pending>();
                _queue[accountId] = list;
            }
            list.Add(new Pending
            {
                Name = label,
                AvatarItemId = 0,
                RewardType = RewardTypeTokens,
                Tokens = tokens,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        }
        return true;
    }

    // A bought consumable (camera film and friends): no wallet item to unlock, so
    // it becomes a collectable reward box the player opens instead.
    public static bool EnqueueConsumable(int accountId, string name, string consumableId)
    {
        if (accountId <= 0) return false;

        lock (_lock)
        {
            if (!_queue.TryGetValue(accountId, out var list))
            {
                list = new List<Pending>();
                _queue[accountId] = list;
            }
            list.Add(new Pending
            {
                Name = name,
                AvatarItemId = 0,
                RewardType = RewardTypeConsumable,
                ConsumableId = consumableId ?? "",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        }
        return true;
    }

    // Catalog indices (0-based) of the items this account has permanently
    // unlocked. The wardrobe treats 1-based item id N as catalog index N-1.
    public static IEnumerable<int> OwnedGiftCatalogIndices(Account acc)
    {
        if (acc?.UnlockedAvatarItemIds == null) yield break;
        var all = AvatarCatalogData.All;
        foreach (int id in acc.UnlockedAvatarItemIds)
        {
            int idx = id - 1;
            if (idx >= 0 && idx < all.Count) yield return idx;
        }
    }

    // Drains the queue and returns the reference pending-rewards shape.
    // Token boxes pay out here, at collect time - this is the client opening
    // the box. Avatar-item gifts already persisted their unlock when queued.
    public static string DrainPendingJson(int accountId)
    {
        List<Pending> drained;
        lock (_lock)
        {
            if (!_queue.TryGetValue(accountId, out var list) || list.Count == 0) return "[]";
            drained = new List<Pending>(list);
            list.Clear();
        }

        int tokensPaid = 0;
        foreach (var g in drained)
            if (g.RewardType == RewardTypeTokens && g.Tokens > 0)
                tokensPaid += g.Tokens;
        if (tokensPaid > 0)
        {
            try { PlayerEconomy.AddTokens(accountId, tokensPaid); }
            catch { }
        }

        var sb = new System.Text.StringBuilder();
        sb.Append('[');
        for (int i = 0; i < drained.Count; i++)
        {
            if (i > 0) sb.Append(',');
            var g = drained[i];
            sb.Append("{\"Id\":\"").Append(Guid.NewGuid().ToString()).Append("\",");
            sb.Append("\"RewardId\":").Append(g.AvatarItemId).Append(',');
            sb.Append("\"RewardType\":").Append(g.RewardType).Append(',');
            // Token boxes carry their amount so the client can label the reward.
            if (g.RewardType == RewardTypeTokens)
                sb.Append("\"Amount\":").Append(g.Tokens).Append(',');
            if (g.RewardType == RewardTypeConsumable)
                sb.Append("\"ConsumableId\":")
                  .Append(System.Text.Json.JsonSerializer.Serialize(g.ConsumableId)).Append(',');
            sb.Append("\"Name\":").Append(System.Text.Json.JsonSerializer.Serialize(g.Name)).Append(',');
            sb.Append("\"CreatedAt\":\"").Append(g.CreatedAt).Append("\",");
            sb.Append("\"IsClaimed\":false}");
        }
        sb.Append(']');
        return sb.ToString();
    }

    // Gift drops are only worth queueing for items that are NOT already in the
    // free starter wardrobe - otherwise a level-up "reward" could land on an
    // item the player already had, and the wardrobe would look unchanged.
    private static int PickRandomOwnedItemId()
    {
        try
        {
            var all = AvatarCatalogData.All;
            if (all == null || all.Count == 0) return 0;

            var candidates = Enumerable.Range(0, all.Count)
                .Where(i => !AvatarFreeItems.IsFreeCatalogIndex(i))
                .ToList();
            if (candidates.Count == 0) return 0;

            lock (_rng) return candidates[_rng.Next(candidates.Count)] + 1;
        }
        catch { return 0; }
    }
}
