namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

// POST /api/items/bulkpurchase
// The "ordinary bag". This route did not exist, which is why sending a gift to
// another player did nothing at all: the client POSTs here and got a 404, so the
// box never left the sender and the receiver's gift list stayed empty.
//
// Reference (CLAUDE.md, client-contract-notes), verbatim on what matters:
//   "A custom line may carry a `Gift` block: ownership and the box go to
//    `ToPlayerId` (announced with `GiftPackageReceivedImmediate`), the buyer still
//    pays and the creator is still paid, and the owned/creator refusals are
//    checked against the RECEIVER - so a creator may gift their own."
//   "`BypassGiftPackages` skips it as it does any line."
//
// So a line is either:
//   - a gift  : { Gift: { ToPlayerId: N } }  -> box AND ownership go to N
//   - ordinary: no Gift, or BypassGiftPackages -> box goes to the buyer
// The buyer is charged either way; this server has no creator payouts, so only the
// box routing differs. Refusals are checked against the RECEIVER, so a player may
// gift an item they already own.
//
// The request shape is taken from buyItem's captured body, which carries the same
// fields: {"PurchasableItemId":N,"CurrencyType":2,"RequestedPrice":N,"Gift":null}.
// Lines may arrive as a JSON array or a single object; "Items" wrapping an array
// is accepted too. Every accepted key is listed in KNOWN_KEYS - an unknown key in
// the request means the contract moved and is reported instead of ignored, because
// guessing a new field is how a purchase silently stops crediting.
//
// The response is the same BalanceUpdates envelope buyItem answers, because the
// reference treats them as the same bag: one debit, one entry, and Data carrying
// the boxes the request produced.
public class BulkPurchaseApi : IApi
{
    private const int UpdateSuccess = 0;
    private const int UpdateDeclined = 303;

    private static readonly string[] KNOWN_KEYS =
    {
        "StorefrontType", "PurchasableItemId", "CurrencyType", "RequestedPrice",
        "CouponConsumablePlayerMappingId", "Gift", "BypassGiftPackages",
        "ItemPurchaseMethodId", "Items", "Data"
    };

    public string Response(HttpContext context)
    {
        int accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;

        string rawText = "";
        try { rawText = AccountHelper.RawBody(context).Trim(); } catch { }

        if (accountId <= 0)
            return Envelope(UpdateDeclined, PlayerEconomy.Balance(accountId), 0, 0, "[]", "NotAuthorized");

        List<JsonElement> lines = ParseLines(rawText, out string error);
        if (lines == null) return Envelope(UpdateDeclined, PlayerEconomy.Balance(accountId), 0, 0, "[]", error);

        // The buyer is charged once per line, and a line that cannot be charged or
        // cannot be delivered is declined on its own so one bad line does not void
        // the rest of the bag.
        int totalPrice = 0;
        var plan = new List<(int itemId, int price, int toAccountId, bool bypass)>();

        foreach (var line in lines)
        {
            if (line.ValueKind != JsonValueKind.Object) continue;

            int itemId = Int(line, "PurchasableItemId");
            if (itemId <= 0) itemId = ItemPurchaseMethodGuidId(line);
            if (itemId <= 0)
            {
                Console.WriteLine($"[bulkpurchase] account={accountId} line without PurchasableItemId " +
                                  $"keys={Keys(line)} -> declined");
                continue;
            }

            var item = StorefrontGiftDropStoreApi.FindItem(itemId);
            int price = item?.Price ?? 0;
            if (price <= 0) price = Int(line, "RequestedPrice");
            if (price < 0) price = 0;

            int toAccountId = ResolveRecipient(line, accountId);
            bool bypass = Bool(line, "BypassGiftPackages");

            Console.WriteLine($"[bulkpurchase] account={accountId} item={itemId} price={price} " +
                              $"to={toAccountId} bypass={bypass} keys={Keys(line)}");

            totalPrice += price;
            plan.Add((itemId, price, toAccountId, bypass));
        }

        if (plan.Count == 0)
            return Envelope(UpdateDeclined, PlayerEconomy.Balance(accountId), 0, totalPrice, "[]", "NoPurchasableItems");

        if (PlayerEconomy.TrySpend(accountId, totalPrice, out int balance) != PlayerEconomy.SpendResult.Ok)
        {
            Console.WriteLine($"[bulkpurchase] account={accountId} DECLINED insufficient balance={balance} total={totalPrice}");
            return Envelope(UpdateDeclined, balance, plan[0].itemId, totalPrice, "[]", "InsufficientFunds");
        }

        // Spend happened first, so any line that fails to deliver has to be
        // refunded. A line to a receiver that does not exist is refused against the
        // RECEIVER (the reference rule) and costs the sender nothing.
        int refunded = 0;
        int grantedCount = 0;
        int lastItemId = plan[0].itemId;

        foreach (var (itemId, price, toAccountId, bypass) in plan)
        {
            lastItemId = itemId;
            var item = StorefrontGiftDropStoreApi.FindItem(itemId);
            if (item == null)
            {
                refunded += price;
                Console.WriteLine($"[bulkpurchase] account={accountId} item={itemId} unknown -> refunded {price}");
                continue;
            }

            if (toAccountId > 0 && AccountHelper.GetAccountById(toAccountId) == null)
            {
                refunded += price;
                Console.WriteLine($"[bulkpurchase] account={accountId} item={itemId} receiver {toAccountId} does not exist -> refunded {price}");
                continue;
            }

            string box = Deliver(toAccountId > 0 ? toAccountId : accountId, accountId, item);
            if (box.Length == 0)
            {
                refunded += price;
                Console.WriteLine($"[bulkpurchase] account={accountId} item={itemId} nothing delivered -> refunded {price}");
                continue;
            }
            grantedCount++;
        }

        int finalBalance = refunded > 0 ? PlayerEconomy.AddTokens(accountId, refunded) : balance;

        if (grantedCount == 0)
            return Envelope(UpdateDeclined, finalBalance, lastItemId, totalPrice, "[]", "NoGrant");

        // Data carries the boxes the BUYER got. When every line was a gift to
        // someone else the buyer has none of their own, so their list is reported
        // as empty - the receiver reads their own /api/avatar/v2/gifts.
        string data = GiftPackageStore.ListJson(accountId);
        Console.WriteLine($"[bulkpurchase] account={accountId} granted={grantedCount} refunded={refunded} balance={finalBalance}");
        return Envelope(UpdateSuccess, finalBalance, lastItemId, totalPrice, data, "");
    }

    // Mints the box and answers its JSON, or "" when the line names neither a
    // wearable nor a consumable (there is nothing the client could open).
    private static string Deliver(int toAccountId, int fromAccountId, StorefrontGiftDropStoreApi.StoreItem item)
    {
        if (item == null) return "";

        if (!string.IsNullOrEmpty(item.AvatarItemDesc))
        {
            var catalogIds = new List<int>();
            var descs = new List<string>();
            foreach (var raw in item.AvatarItemDesc.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var guid = raw.Trim();
                if (guid.Length != 36) continue;
                int idx = AvatarCatalogData.IndexOfGuid(guid);
                if (idx < 0) continue;
                catalogIds.Add(idx + 1);
                descs.Add(guid);
            }
            if (catalogIds.Count == 0) return "";
            return GiftPackageStore.MintAvatarItem(toAccountId, fromAccountId, item.FriendlyName,
                string.Join(",", descs), catalogIds).ToJson();
        }

        if (!string.IsNullOrEmpty(item.ConsumableItemDesc))
            return GiftPackageStore.MintConsumable(toAccountId, fromAccountId, item.FriendlyName,
                item.ConsumableItemDesc).ToJson();

        return "";
    }

    // The receiver. The reference names the field ToPlayerId; playerId/toPlayerId
    // are accepted alongside it because the sender id itself arrives as
    // fromPlayerId in some client builds. Falls back to the buyer when no Gift
    // block is present, which is an ordinary purchase.
    private static int ResolveRecipient(JsonElement line, int buyerId)
    {
        foreach (var key in new[] { "Gift", "gift" })
        {
            if (!line.TryGetProperty(key, out var gift)) continue;
            if (gift.ValueKind == JsonValueKind.Number && gift.TryGetInt32(out int direct) && direct > 0)
                return direct;
            if (gift.ValueKind != JsonValueKind.Object) continue;

            foreach (var f in new[] { "ToPlayerId", "toPlayerId", "PlayerId", "playerId" })
                if (gift.TryGetProperty(f, out var v) && v.TryGetInt32(out int id) && id > 0)
                    return id;
        }

        foreach (var f in new[] { "ToPlayerId", "toPlayerId" })
            if (line.TryGetProperty(f, out var v) && v.TryGetInt32(out int id) && id > 0)
                return id;

        return buyerId;
    }

    // A custom avatar item can be named by its CustomAvatarItemId instead of a
    // storefront id: the reference's line is
    // {"ItemPurchaseMethodId":{"Type":1,"Guid":"<CustomAvatarItemId>"}}. This
    // server's catalog is first-party prefab rows keyed by PurchasableItemId, and
    // there is no captured CustomAvatarItemId -> store item mapping to resolve a
    // guid against, so the line is reported rather than guessed at.
    private static int ItemPurchaseMethodGuidId(JsonElement line)
    {
        if (!line.TryGetProperty("ItemPurchaseMethodId", out var m)) return 0;
        if (m.ValueKind != JsonValueKind.Object) return 0;
        if (m.TryGetProperty("Type", out var t) && t.TryGetInt32(out int type) && type != 0)
        {
            string guid = m.TryGetProperty("Guid", out var g) && g.ValueKind == JsonValueKind.String ? g.GetString() : "";
            Console.WriteLine($"[bulkpurchase] line names ItemPurchaseMethodId Type={type} Guid='{guid}' " +
                               $"- no CustomAvatarItemId -> store item mapping exists, refusing to guess");
        }
        return 0;
    }

    private static List<JsonElement> ParseLines(string rawText, out string error)
    {
        error = "";
        var lines = new List<JsonElement>();
        if (string.IsNullOrWhiteSpace(rawText)) { error = "EmptyBody"; return null; }

        try
        {
            using var doc = JsonDocument.Parse(rawText);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in root.EnumerateArray()) lines.Add(el.Clone());
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // A wrapped array, if the client ever posts one.
                foreach (var wrap in new[] { "Items", "Data" })
                    if (root.TryGetProperty(wrap, out var w) && w.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in w.EnumerateArray()) lines.Add(el.Clone());
                        break;
                    }
                if (lines.Count == 0) lines.Add(root.Clone());
            }

            return lines;
        }
        catch (Exception ex)
        {
            error = "UnparseableBody";
            Console.WriteLine($"[bulkpurchase] body unparseable: {ex.Message}");
            return null;
        }
    }

    private static string Envelope(int updateResponse, int balance, int itemId, int totalPrice, string dataJson, string error)
    {
        var sb = new StringBuilder();
        sb.Append("{\"BalanceUpdates\":[{\"UpdateResponse\":").Append(updateResponse);
        sb.Append(",\"Data\":").Append(string.IsNullOrEmpty(dataJson) ? "[]" : dataJson);
        sb.Append(",\"PurchasableItemId\":").Append(itemId);
        sb.Append(",\"Balance\":").Append(balance);
        sb.Append(",\"CurrencyType\":2,\"BalanceType\":-2}]");
        sb.Append(",\"Balance\":").Append(balance);
        sb.Append(",\"CurrencyType\":2,\"BalanceType\":-2");
        sb.Append(",\"PurchasableItemId\":").Append(itemId);
        sb.Append(",\"TotalPrice\":").Append(totalPrice);
        if (error.Length > 0) sb.Append(",\"Error\":").Append(Json(error));
        sb.Append(",\"ErrorCode\":").Append(updateResponse == UpdateSuccess ? 0 : 1);
        sb.Append('}');
        return sb.ToString();
    }

    private static int Int(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.TryGetInt32(out int i) ? i : 0;

    private static bool Bool(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var v)) return false;
        return v.ValueKind == JsonValueKind.True ||
               (v.ValueKind == JsonValueKind.String && string.Equals(v.GetString(), "true", StringComparison.OrdinalIgnoreCase));
    }

    private static string Keys(JsonElement el)
    {
        var names = new List<string>();
        foreach (var p in el.EnumerateObject())
        {
            names.Add(p.Name);
            if (!KNOWN_KEYS.Contains(p.Name))
                Console.WriteLine($"[bulkpurchase] UNKNOWN request key '{p.Name}' - the contract may have moved");
        }
        return string.Join(",", names);
    }

    private static string Json(string s) => JsonSerializer.Serialize(s);

    public string GetUrl() => "/api/items/bulkpurchase";
    public string GetMethod() => HttpMethods.Post;
}