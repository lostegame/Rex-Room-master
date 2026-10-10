namespace Rex_Room_20230414.API;

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

// A gift box is the ONLY way the client learns about a purchase or a gift. The
// reference documents this in two places that matter here:
//
//   CLAUDE.md, client-contract-notes:
//   "The item still comes in a gift box, like any purchase: its GiftPackage names
//    it by CustomAvatarItemId ... without a box the purchase shows the player
//    nothing."
//   "A gift box's AvatarItemType (econ: the stored box in GET /api/avatar/v2/gifts,
//    buyItem's BalanceUpdates[].Data, bulkpurchase's GiftPackage, and the
//    GiftPackageReceived* frame ...) must be NULL for anything that is not an
//    avatar item - a consumable, a skin, a box, tokens, XP - and a NUMBER (the
//    type, or 0) when the box holds one. The client routes the box on that field
//    before it reads what else the box names, so a 0 sends it looking for avatar
//    item type 0 and it fails with 'can't find avatar item'."
//
// The field set is not guessed: it is the reference's own GiftPackagePayload
// (apps/notify/src/notification-payloads.ts), the payload the GiftPackageReceived
// (30) and GiftPackageReceivedImmediate (31) frames carry. Keep the two in step.
//
// Every box is minted through Mint* below, so the null-vs-number AvatarItemType
// rule is applied in exactly one place.
public class GiftPackage
{
    public int? Id { get; set; }
    public int? FromPlayerId { get; set; }
    public string ConsumableItemDesc { get; set; } = "";
    public int? AvatarItemType { get; set; }
    public string AvatarItemDesc { get; set; } = "";
    public string EquipmentPrefabName { get; set; } = "";
    public string EquipmentModificationGuid { get; set; } = "";
    public int CurrencyType { get; set; }
    public int Currency { get; set; }
    public int Xp { get; set; }
    public int GiftContext { get; set; }
    public int GiftRarity { get; set; }
    public string Message { get; set; } = "";
    public int Platform { get; set; }
    public int? BalanceType { get; set; }

    // Not part of the reference payload. Bookkeeping this server needs to stop
    // hand-granting the same wearable twice when the box is opened, kept out of
    // ToJson() so the client never sees it.
    public List<int> AvatarItemIds { get; set; } = new List<int>();

    public string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append('{');
        AppendRaw(sb, "Id", Id.HasValue ? Id.Value.ToString() : "null");
        sb.Append(',');
        AppendRaw(sb, "FromPlayerId", FromPlayerId.HasValue ? FromPlayerId.Value.ToString() : "null");
        sb.Append(',');
        AppendRaw(sb, "ConsumableItemDesc", Json(ConsumableItemDesc));
        sb.Append(',');
        // null (NOT 0) whenever the box holds anything that is not an avatar item.
        AppendRaw(sb, "AvatarItemType", AvatarItemType.HasValue ? AvatarItemType.Value.ToString() : "null");
        sb.Append(',');
        AppendRaw(sb, "AvatarItemDesc", Json(AvatarItemDesc));
        sb.Append(',');
        AppendRaw(sb, "EquipmentPrefabName", Json(EquipmentPrefabName));
        sb.Append(',');
        AppendRaw(sb, "EquipmentModificationGuid", Json(EquipmentModificationGuid));
        sb.Append(',');
        AppendRaw(sb, "CurrencyType", CurrencyType.ToString());
        sb.Append(',');
        AppendRaw(sb, "Currency", Currency.ToString());
        sb.Append(',');
        AppendRaw(sb, "Xp", Xp.ToString());
        sb.Append(',');
        AppendRaw(sb, "GiftContext", GiftContext.ToString());
        sb.Append(',');
        AppendRaw(sb, "GiftRarity", GiftRarity.ToString());
        sb.Append(',');
        AppendRaw(sb, "Message", Json(Message));
        sb.Append(',');
        AppendRaw(sb, "Platform", Platform.ToString());
        sb.Append(',');
        // "PlatformsToSpawnOn: unknown" in the reference; the spawn list is how a
        // box turns into the object in the player's hands. Omitted rather than
        // guessed: a wrong spawn list is worse than none, and the reference
        // records no value for it.
        AppendRaw(sb, "BalanceType", BalanceType.HasValue ? BalanceType.Value.ToString() : "null");
        sb.Append('}');
        return sb.ToString();
    }

    private static void AppendRaw(StringBuilder sb, string key, string rawJson)
    {
        sb.Append('"').Append(key).Append("\":").Append(rawJson);
    }

    private static string Json(string s) => System.Text.Json.JsonSerializer.Serialize(s ?? "");
}

// Per-account store of unopened boxes. Persisted on the Account so a box survives
// a server restart, which the old in-memory ProgressionHelper queue did not.
public static class GiftPackageStore
{
    private static readonly ConcurrentDictionary<int, object> Locks = new ConcurrentDictionary<int, object>();

    // A box naming an avatar wearable. avatarItemType must be a NUMBER here, and
    // the catalog index+1 ids ride along so opening the box can unlock the item
    // instead of the purchase having done it behind the player's back.
    public static GiftPackage MintAvatarItem(int toAccountId, int fromAccountId, string friendlyName,
        string avatarItemDesc, IReadOnlyList<int> catalogItemIds, int rarity = 0, int context = 0)
    {
        var box = new GiftPackage
        {
            AvatarItemType = 0,
            AvatarItemDesc = avatarItemDesc ?? "",
            EquipmentPrefabName = "",
            EquipmentModificationGuid = "",
            Message = friendlyName ?? "",
            GiftRarity = rarity,
            GiftContext = context,
            CurrencyType = 0,
            Currency = 0,
            AvatarItemIds = catalogItemIds == null ? new List<int>() : new List<int>(catalogItemIds)
        };
        Attach(toAccountId, fromAccountId, box);
        return box;
    }

    // A box holding something that is NOT an avatar item - a consumable, a box,
    // tokens, XP. avatarItemType is null on purpose: the client routes the box on
    // this field before anything else, and 0 would send it hunting for avatar item
    // type 0 and fail with "can't find avatar item".
    public static GiftPackage MintConsumable(int toAccountId, int fromAccountId, string friendlyName,
        string consumableItemDesc, int currency = 0, int currencyType = 0, int context = 0)
    {
        var box = new GiftPackage
        {
            AvatarItemType = null,
            ConsumableItemDesc = consumableItemDesc ?? "",
            Message = friendlyName ?? "",
            GiftContext = context,
            Currency = currency,
            CurrencyType = currencyType,
            EquipmentPrefabName = "",
            EquipmentModificationGuid = ""
        };
        Attach(toAccountId, fromAccountId, box);
        return box;
    }

    private static void Attach(int toAccountId, int fromAccountId, GiftPackage box)
    {
        if (toAccountId <= 0) return;
        var acc = AccountHelper.GetAccountById(toAccountId);
        if (acc == null) return;
        if (acc.GiftPackages == null) acc.GiftPackages = new List<GiftPackage>();
        lock (Locks.GetOrAdd(toAccountId, _ => new object()))
        {
            box.FromPlayerId = fromAccountId > 0 ? fromAccountId : (int?)null;
            box.Id = NextId(acc);
            acc.GiftPackages.Add(box);
            GrantWardrobeOwnership(acc, box.AvatarItemDesc);
        }
        AccountHelper.SaveAccount(acc);
        Console.WriteLine($"[giftPackage] to={toAccountId} from={fromAccountId} id={box.Id} " +
                          $"avatarItemType={(box.AvatarItemType.HasValue ? box.AvatarItemType.Value.ToString() : "null")} " +
                          $"desc='{box.AvatarItemDesc}' consumable='{box.ConsumableItemDesc}'");
    }

    // Buying an avatar item has to grant it to the wardrobe IMMEDIATELY.
    //
    // /econ/customAvatarItems/v1/owned is built purely from UnlockedAvatarItemIds,
    // and only the free starter set arrives any other way (via
    // /api/avatar/v1/defaultunlocked). So when the unlock was deferred to
    // POST /api/avatar/v2/gifts/consume/, the paid pieces never reached the
    // wardrobe at all - CUSTOMIZE showed the free items only, and the client
    // never issues that consume call on its own for a normal purchase.
    // The box is still attached, because it is what puts the piece in the room.
    private static void GrantWardrobeOwnership(Account acc, string avatarItemDesc)
    {
        if (acc == null || string.IsNullOrEmpty(avatarItemDesc)) return;
        if (acc.UnlockedAvatarItemIds == null) acc.UnlockedAvatarItemIds = new List<int>();

        // "guid,combo,variant,,bodyPart" - possibly a multi-guid desc; the first
        // entry is the prefab the client spawned.
        string guid = avatarItemDesc.Split(',')[0].Trim();
        if (guid.Length != 36) return;

        var all = AvatarCatalogData.All;
        for (int i = 0; i < all.Count; i++)
        {
            if (!string.Equals(all[i].Guid, guid, System.StringComparison.OrdinalIgnoreCase)) continue;
            int id = i + 1;
            if (acc.UnlockedAvatarItemIds.Contains(id)) return;
            acc.UnlockedAvatarItemIds.Add(id);
            Console.WriteLine($"[giftPackage] granted wardrobe id={id} '{all[i].Name}' to account {acc.Id}");
            return;
        }
        Console.WriteLine($"[giftPackage] desc guid {guid} not in catalog - ownership NOT granted");
    }

    // Ids are per account and never reused, so a consumed box cannot be confused
    // with a later one in a gift.manualconsumed frame.
    private static int NextId(Account acc)
    {
        int max = 0;
        if (acc.GiftPackages != null)
            foreach (var b in acc.GiftPackages)
                if (b.Id.HasValue && b.Id.Value > max) max = b.Id.Value;
        return max + 1;
    }

    public static string ListJson(int accountId)
    {
        var acc = AccountHelper.GetAccountById(accountId);
        if (acc?.GiftPackages == null || acc.GiftPackages.Count == 0) return "[]";
        var sb = new StringBuilder();
        sb.Append('[');
        for (int i = 0; i < acc.GiftPackages.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(acc.GiftPackages[i].ToJson());
        }
        sb.Append(']');
        return sb.ToString();
    }

    // Opens a box: removes it from the store and hands it back so the caller can
    // unlock what it names. Returns null when the id is not this player's.
    public static GiftPackage Consume(int accountId, int giftPackageId)
    {
        var acc = AccountHelper.GetAccountById(accountId);
        if (acc?.GiftPackages == null) return null;

        GiftPackage found = null;
        lock (Locks.GetOrAdd(accountId, _ => new object()))
        {
            for (int i = 0; i < acc.GiftPackages.Count; i++)
            {
                if (acc.GiftPackages[i].Id == giftPackageId) { found = acc.GiftPackages[i]; break; }
            }
            if (found != null)
            {
                acc.GiftPackages.Remove(found);
                AccountHelper.SaveAccount(acc);
            }
        }
        if (found != null)
            Console.WriteLine($"[giftPackage] account={accountId} consumed id={giftPackageId} avatarItemType=" +
                              $"{(found.AvatarItemType.HasValue ? found.AvatarItemType.Value.ToString() : "null")}");
        return found;
    }
}