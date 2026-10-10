namespace Rex_Room_20230414.API;

using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

// The per-entry Dorm reward: a box worth 100 tokens plus a random gift, granted
// once each time the player enters their Dorm. XP is granted too, because the
// 2023 client never posts to /api/players/v2/addxp - so before this, XP never
// moved, the level never rose, and the existing level-up gift drop in
// ProgressionHelper could never fire.
//
// The 100 tokens are NOT added to the balance here. They are queued as a gift
// box (GiftDrop.EnqueueTokens) that pays out when the client collects
// GET /api/gamerewards/v1/pending, so the player opens a box instead of watching
// the balance silently jump by 100 on entry.
//
// Once-per-entry is keyed on LoginLock + roomInstanceId rather than on the
// heartbeat, so the reward follows an actual room transition and is not paid out
// on every heartbeat tick or on every re-request of the same matchmake.
public static class JoinReward
{
    private sealed class Seen : IEquatable<Seen>
    {
        public string Lock = "";
        public int InstanceId;
        public bool Equals(Seen o) => o != null && o.InstanceId == InstanceId && o.Lock == Lock;
        public override int GetHashCode() => HashCode.Combine(Lock, InstanceId);
    }

    private static readonly ConcurrentDictionary<int, Seen> _lastEntry = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Grants the reward when this (account, loginLock, roomInstance) triple has
    /// not been rewarded yet. Returns a short label describing what was given,
    /// or null when nothing was granted (repeat entry / unknown account).
    /// Only Dorm entries pay out; every other room returns null.
    /// </summary>
    public static string Grant(HttpContext context, int accountId, int roomInstanceId, bool isDorm)
    {
        if (accountId <= 0) return null;
        if (!isDorm) return null;

        string loginLock = context.Request.Query["LoginLock"].ToString();
        if (string.IsNullOrEmpty(loginLock))
        {
            try
            {
                context.Request.EnableBuffering();
                using var sr = new System.IO.StreamReader(context.Request.Body);
                sr.ReadToEnd();
                context.Request.Body.Position = 0;
                loginLock = context.Request.Form["LoginLock"].ToString();
            }
            catch { }
        }
        if (string.IsNullOrEmpty(loginLock)) loginLock = "nologinlock";

        var seen = new Seen { Lock = loginLock, InstanceId = roomInstanceId };

        // Only pay once for this exact entry.
        if (_lastEntry.TryGetValue(accountId, out var prev) && prev.Equals(seen)) return null;

        lock (_lock)
        {
            if (_lastEntry.TryGetValue(accountId, out var prev2) && prev2.Equals(seen)) return null;
            _lastEntry[accountId] = seen;
        }

        // Tokens go out as a box, not as a balance deposit. The balance only
        // moves when the client collects the pending reward.
        int tokens = PlayerEconomy.JoinRewardTokens;
        int balance = PlayerEconomy.Balance(accountId);
        if (!GiftDrop.EnqueueTokens(accountId, tokens, "Dorm Entry Reward")) return null;

        // Random gift, same helper the level-up path uses. It also persists the
        // unlock so it shows up in the wardrobe.
        string gift = "";
        try
        {
            int before = AccountHelper.GetAccountById(accountId)?.UnlockedAvatarItemIds?.Count ?? 0;
            GiftDrop.EnqueueFor(accountId, "Room Entry Reward");
            int after = AccountHelper.GetAccountById(accountId)?.UnlockedAvatarItemIds?.Count ?? 0;
            if (after > before) gift = " +gift#" + after;
        }
        catch { }

        // Level-up may fire from here too, queueing its own gift on top.
        string levelNote = "";
        try
        {
            var acc = AccountHelper.GetAccountById(accountId);
            if (acc != null)
            {
                acc.LastRewardLoginLock = loginLock;
                acc.LastRewardAt = DateTime.UtcNow.ToString("O");
                AccountHelper.SaveAccount(acc);
                int levels = ProgressionHelper.AddXp(acc, PlayerEconomy.JoinRewardXp);
                if (levels > 0) levelNote = $" +{levels}level";
            }
        }
        catch { }

        Console.WriteLine($"[joinReward] account={accountId} +{tokens}tokens(box){gift}{levelNote} balance={balance} roomInstance={roomInstanceId}");
        return $"{tokens}(box){gift}{levelNote}";
    }

    // Called on logout so the next login is treated as a fresh entry.
    public static void Reset(int accountId)
    {
        _lastEntry.TryRemove(accountId, out _);
    }
}
