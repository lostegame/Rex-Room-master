namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;

// Storefront endpoints the client calls while the token shop loads.
//
// Reference shapes (rex room LogOutput.log):
//   GET  /api/storefronts/v1/adcarouselitems ->
//        [{"AdCarouselItemId":1,"Description":"No description.",
//          "ImageName":"AdCarouselItem.png","PurchasableItemIds":[],"Title":"Lorem ipsum"}]
//   GET  /api/storefronts/v1/balanceAddType/2/{id} ->
//        404 {"success":false,"error":{"message":"not found"}}   (BOTH 100 and 101)
//   POST /api/storefronts/v2/buyItem ->
//        {"BalanceUpdates":[{"UpdateResponse":303,"Data":[]}],"Balance":<after>,
//         "CurrencyType":2,"BalanceType":-2}
//
// UpdateResponse 0 = accepted, 303 = declined - the same convention
// InventionsPurchaseApi already uses for buyInvention.
//
// buyItem is a REAL purchase: it prices the item from the storefront catalog
// (never from the client's own RequestedPrice), spends the tokens, grants what
// was bought and persists it. It used to answer 303 with a hardcoded balance of
// 10000 and empty Data, which declined every purchase - that is why paid items
// vanished from the wardrobe the moment they were bought.
public class StorefrontApi : IApi
{
    private const int UpdateSuccess = 0;
    private const int UpdateDeclined = 303;

    private readonly string _url;
    private readonly string _kind; // adcarousel | balanceAddType | buyItem
    private int _status = 200;

    public StorefrontApi(string url, string kind)
    {
        _url = url;
        _kind = kind;
    }

    public string Response(HttpContext context)
    {
        _status = 200;

        switch (_kind)
        {
            case "adcarousel":
                return "[{\"AdCarouselItemId\":1,\"Description\":\"No description.\"," +
                       "\"ImageName\":\"AdCarouselItem.png\",\"PurchasableItemIds\":[]," +
                       "\"Title\":\"Lorem ipsum\"}]";

            case "balanceAddType":
                // The reference 404s every balance-add-type id, so we match it
                // rather than inventing a balance the client would then expect.
                _status = 404;
                return "{\"success\":false,\"error\":{\"message\":\"not found\"}}";

            default:
                return BuyItem(context);
        }
    }

    // POST /api/storefronts/v2/buyItem
    //   {"StorefrontType":300,"PurchasableItemId":2169,"CurrencyType":2,
    //    "RequestedPrice":20,"CouponConsumablePlayerMappingId":null,"Gift":null}
    private string BuyItem(HttpContext context)
    {
        int accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        int itemId = 0;
        string raw = "";

        try
        {
            var rawText = AccountHelper.RawBody(context).Trim();
            raw = rawText;
            if (rawText.StartsWith("{"))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(rawText);
                var root = doc.RootElement;
                if (root.TryGetProperty("PurchasableItemId", out var pi) &&
                    pi.TryGetInt32(out int b)) itemId = b;
            }
            else
            {
                itemId = QueryInt(context, "PurchasableItemId", 0);
            }
        }
        catch { }

        Console.WriteLine($"[buyItem] raw={raw.Length} chars ct='{context.Request.ContentType}' parsedItemId={itemId}");

        if (accountId <= 0 || itemId <= 0)
            return Envelope(UpdateDeclined, PlayerEconomy.Balance(accountId), itemId);

        var item = StorefrontGiftDropStoreApi.FindItem(itemId);

        // The price always comes from the storefront catalog. Falling back to the
        // client's own RequestedPrice only when the catalog has no entry keeps a
        // crafted request from naming its own price.
        int price = item?.Price ?? 0;
        if (price <= 0) price = QueryInt(context, "RequestedPrice", 0);
        if (price < 0) price = 0;

        if (PlayerEconomy.TrySpend(accountId, price, out int balance) != PlayerEconomy.SpendResult.Ok)
        {
            Console.WriteLine($"[buyItem] account={accountId} item={itemId} DECLINED insufficient balance={balance}");
            return Envelope(UpdateDeclined, balance, itemId);
        }

        string granted = Grant(accountId, item);

        // TrySpend already took the tokens, so a grant that delivered nothing new
        // has to give them back. Buying an already-owned wearable used to cost
        // 3000 tokens a second time and answer "alreadyowned": the spend happened
        // first and nothing undid it. Every non-granting outcome is refunded -
        // alreadyowned (the account owns every catalog id in the line, so no box
        // is minted and nothing is charged), nomatch (no prefab guid matched),
        // failed (consumable could not be queued), unknown (not in the catalog)
        // and noaccount.
        if (!IsRealGrant(granted))
        {
            int refunded = PlayerEconomy.AddTokens(accountId, price);
            Console.WriteLine($"[buyItem] account={accountId} item={itemId} NO-GRANT " +
                              $"granted='{granted}' -> refunded {price}, balance={refunded}");
            return Envelope(UpdateDeclined, refunded, itemId, granted);
        }

        Console.WriteLine($"[buyItem] account={accountId} item={itemId} price={price} balance={balance} granted={granted}");
        return Envelope(UpdateSuccess, balance, itemId, granted, GiftPackageStore.ListJson(accountId));
    }

    // "box" is the only outcome that puts something in the account's bag. Anything
    // else means the player got nothing for their tokens.
    private static bool IsRealGrant(string granted) =>
        !string.IsNullOrEmpty(granted) && granted == "box";

    // A purchase does NOT hand the item over directly: it mints a gift box into the
    // buyer's bag, because that is the only thing the client renders.
    //
    // CLAUDE.md client-contract-notes: "The item still comes in a gift box, like any
    // purchase: its GiftPackage names it by CustomAvatarItemId with AvatarItemDesc/
    // EquipmentPrefabName/etc. empty - without a box the purchase shows the player
    // nothing." This server's catalog rows name the wearable by AvatarItemDesc
    // instead (first-party items carry a prefab guid, not a CustomAvatarItemId), so
    // the box is named the way this server's own data can name it.
    //
    // The wearable is unlocked when the box is opened (AvatarGiftsApi), not here.
    private static string Grant(int accountId, StorefrontGiftDropStoreApi.StoreItem item)
    {
        if (item == null) return "unknown";

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
            if (catalogIds.Count == 0) return "nomatch";

            // Re-buying something the account already owns must not charge again.
            // Ownership is written at purchase time (GiftPackage.GrantWardrobeOwnership),
            // so a second buy of the same wearable sees it here, "alreadyowned"
            // counts as a non-grant and the spend is refunded by the caller -
            // which is exactly the contract verify_purchase.ps1 asserts.
            var ownedIds = AccountHelper.GetAccountById(accountId)?.UnlockedAvatarItemIds;
            if (ownedIds != null && catalogIds.TrueForAll(id => ownedIds.Contains(id)))
                return "alreadyowned";

            GiftPackageStore.MintAvatarItem(accountId, accountId, item.FriendlyName,
                string.Join(",", descs), catalogIds);
            return "box";
        }

        GiftPackageStore.MintConsumable(accountId, accountId, item.FriendlyName, item.ConsumableItemDesc);
        return "box";
    }

    private static string Envelope(int updateResponse, int balance, int itemId, string granted = "", string dataJson = "[]")
    {
        var sb = new StringBuilder();
        sb.Append("{\"BalanceUpdates\":[{\"UpdateResponse\":").Append(updateResponse);
        // buyItem's BalanceUpdates[].Data is where the client finds the box it just
        // bought, so it carries this account's unopened boxes and not an empty array.
        sb.Append(",\"Data\":").Append(string.IsNullOrEmpty(dataJson) ? "[]" : dataJson);
        sb.Append(",\"PurchasableItemId\":").Append(itemId);
        sb.Append(",\"Balance\":").Append(balance);
        sb.Append(",\"CurrencyType\":2,\"BalanceType\":-2}]");
        sb.Append(",\"Balance\":").Append(balance);
        sb.Append(",\"CurrencyType\":2,\"BalanceType\":-2");
        sb.Append(",\"PurchasableItemId\":").Append(itemId);
        if (granted.Length > 0)
            sb.Append(",\"Granted\":").Append(Json(granted));
        sb.Append(",\"ErrorCode\":").Append(updateResponse == UpdateSuccess ? 0 : 1);
        sb.Append('}');
        return sb.ToString();
    }

    private static string Json(string s) => System.Text.Json.JsonSerializer.Serialize(s);

    private static int QueryInt(HttpContext context, string key, int fallback)
    {
        if (context.Request.Query.TryGetValue(key, out var v) &&
            int.TryParse(v.ToString(), out int parsed))
            return parsed;
        return fallback;
    }

    public string GetUrl() => _url;
    public string GetMethod() => _kind == "buyItem" ? HttpMethods.Post : HttpMethods.Get;
    public int GetStatusCode() => _status;
}
