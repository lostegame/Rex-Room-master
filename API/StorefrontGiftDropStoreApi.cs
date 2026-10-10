namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;

public class StorefrontGiftDropStoreApi : IApi
{
    // Client expectations (from IL2CPP decompile, dump.cs):
    //   FFGPLFHAGKC (storefront response) : JJAJHFHIHIO (StorefrontType datetime NextUpdate)
    //     -> List<EOKBGGOAFLF> StoreItems  (EOKBGGOAFLF : IBKACDLFPML)
    //   Each StoreItem carries GiftDrop, IsFeatured, Prices, PurchasableItemId,
    //   SubscriberPrices, Type. The response root is an OBJECT {NextUpdate, StoreItems},
    //   verified identical to working recflare captures for ids 3, 2000-2004.
    //
    //   - ids 2000-2004  -> ONE store item (Archer Quiver gift, tooltip " Debug: SF {id}")
    //   - id 3 / default -> the consumables gift store (captured item list)

    // Debug storefront (sf 2000) single-item body; "SF 2000" patched per requested id.
    private const string DebugStorefrontTemplate = """{"NextUpdate":"2226-06-14T00:12:20.1324853Z","StoreItems":[{"GiftDrop":{"AvatarItemDesc":"5b6535f0-86cd-417a-bcce-a1ace9a5f260,imDGL6dhrka-KliYlfpdgw,6094370d-5e28-473a-b422-f3f1c75d5db8,","AvatarItemType":0,"ConsumableItemDesc":"","Context":0,"Currency":0,"CurrencyType":0,"EquipmentModificationGuid":"","EquipmentPrefabName":"","FriendlyName":"Archer Quiver (Grey)","GiftDropId":7,"IsQuery":false,"ItemSetFriendlyName":"","ItemSetId":0,"Rarity":50,"SubscribersOnly":false,"Tooltip":" Debug: SF 2000","Unique":true},"IsFeatured":false,"Prices":[{"CurrencyType":2,"Price":3000,"StorefrontSaleData":null,"Type":0}],"PurchasableItemId":7,"SubscriberPrices":[{"CurrencyType":2,"Price":3000,"StorefrontSaleData":null,"Type":0}],"Type":0}]}""";

    // Consumable gift store (sf 3) captured body (valid items only; the client log
    // truncated the tail, so this is the fully-captured subset of the real catalog).
    private const string ConsumableStorefrontBody = """{"NextUpdate":"2226-06-14T00:12:20.1324853Z","StoreItems":[{"GiftDrop":{"AvatarItemDesc":"","AvatarItemType":0,"ConsumableItemDesc":"frOMH6WxDEG1fBqC4_83vg","Context":110000,"Currency":0,"CurrencyType":0,"EquipmentModificationGuid":"","EquipmentPrefabName":"","FriendlyName":"Film (Black & White)","GiftDropId":2168,"IsQuery":false,"ItemSetFriendlyName":"","ItemSetId":0,"Rarity":0,"SubscribersOnly":false,"Tooltip":"Take some black and white photos. Debug: 2168","Unique":false},"IsFeatured":false,"Prices":[{"CurrencyType":2,"Price":20,"StorefrontSaleData":null,"Type":0}],"PurchasableItemId":2168,"SubscriberPrices":[{"CurrencyType":2,"Price":20,"StorefrontSaleData":null,"Type":0}],"Type":0},{"GiftDrop":{"AvatarItemDesc":"","AvatarItemType":0,"ConsumableItemDesc":"m0bVLwWGj0GuIBSb6wCk6Q","Context":110000,"Currency":0,"CurrencyType":0,"EquipmentModificationGuid":"","EquipmentPrefabName":"","FriendlyName":"Film (Dawn)","GiftDropId":2169,"IsQuery":false,"ItemSetFriendlyName":"","ItemSetId":0,"Rarity":0,"SubscribersOnly":false,"Tooltip":"Take some sun-soaked photos. Debug: 2169","Unique":false},"IsFeatured":false,"Prices":[{"CurrencyType":2,"Price":20,"StorefrontSaleData":null,"Type":0}],"PurchasableItemId":2169,"SubscriberPrices":[{"CurrencyType":2,"Price":20,"StorefrontSaleData":null,"Type":0}],"Type":0},{"GiftDrop":{"AvatarItemDesc":"","AvatarItemType":0,"ConsumableItemDesc":"A5M-yf9tgUihq1uab3v58g","Context":110000,"Currency":0,"CurrencyType":0,"EquipmentModificationGuid":"","EquipmentPrefabName":"","FriendlyName":"Film (Sepia)","GiftDropId":2174,"IsQuery":false,"ItemSetFriendlyName":"","ItemSetId":0,"Rarity":0,"SubscribersOnly":false,"Tooltip":"Take some old-timey photos. Debug: 2174","Unique":false},"IsFeatured":false,"Prices":[{"CurrencyType":2,"Price":20,"StorefrontSaleData":null,"Type":0}],"PurchasableItemId":2174,"SubscriberPrices":[{"CurrencyType":2,"Price":20,"StorefrontSaleData":null,"Type":0}],"Type":0},{"GiftDrop":{"AvatarItemDesc":"","AvatarItemType":0,"ConsumableItemDesc":"-hy0qD-iUk-v4NHxNzanmg","Context":4004,"Currency":0,"CurrencyType":0,"EquipmentModificationGuid":"","EquipmentPrefabName":"","FriendlyName":"High Five Potion (Golden)","GiftDropId":2176,"IsQuery":false,"ItemSetFriendlyName":"","ItemSetId":0,"Rarity":0,"SubscribersOnly":false,"Tooltip":"Give golden high fives for a time. Debug: 2176","Unique":false},"IsFeatured":false,"Prices":[{"CurrencyType":2,"Price":30,"StorefrontSaleData":null,"Type":0}],"PurchasableItemId":2176,"SubscriberPrices":[{"CurrencyType":2,"Price":30,"StorefrontSaleData":null,"Type":0}],"Type":0},{"GiftDrop":{"AvatarItemDesc":"","AvatarItemType":0,"ConsumableItemDesc":"VQSgL2pTLkWx4B3kwYG7UA","Context":4014,"Currency":0,"CurrencyType":0,"EquipmentModificationGuid":"","EquipmentPrefabName":"","FriendlyName":"High Five Potion (Magic)","GiftDropId":2179,"IsQuery":false,"ItemSetFriendlyName":"","ItemSetId":0,"Rarity":0,"SubscribersOnly":false,"Tooltip":"Give magical high fives for a time. Debug: 2179","Unique":false},"IsFeatured":false,"Prices":[{"CurrencyType":2,"Price":30,"StorefrontSaleData":null,"Type":0}],"PurchasableItemId":2179,"SubscriberPrices":[{"CurrencyType":2,"Price":30,"StorefrontSaleData":null,"Type":0}],"Type":0},{"GiftDrop":{"AvatarItemDesc":"","AvatarItemType":0,"ConsumableItemDesc":"xwQWBB_fekmTqRc2LB92cg","Context":110000,"Currency":0,"CurrencyType":0,"EquipmentModificationGuid":"","EquipmentPrefabName":"","FriendlyName":"87 Flavor Cake","GiftDropId":2181,"IsQuery":false,"ItemSetFriendlyName":"","ItemSetId":0,"Rarity":30,"SubscribersOnly":false,"Tooltip":"Eight slices of tasty cake to share with friends. Debug: 2181","Unique":false},"IsFeatured":false,"Prices":[{"CurrencyType":2,"Price":600,"StorefrontSaleData":null,"Type":0}],"PurchasableItemId":2181,"SubscriberPrices":[{"CurrencyType":2,"Price":600,"StorefrontSaleData":null,"Type":0}],"Type":0},{"GiftDrop":{"AvatarItemDesc":"","AvatarItemType":0,"ConsumableItemDesc":"ZuvkidodzkuOfGLDnTOFyg","Context":110000,"Currency":0,"CurrencyType":0,"EquipmentModificationGuid":"","EquipmentPrefabName":"","FriendlyName":"Assorted Donuts","GiftDropId":2182,"IsQuery":false,"ItemSetFriendlyName":"","ItemSetId":0,"Rarity":20,"SubscribersOnly":false,"Tooltip":"A dozen assorted donuts to share with friends. Debug: 2182","Unique":false},"IsFeatured":false,"Prices":[{"CurrencyType":2,"Price":100,"StorefrontSaleData":null,"Type":0}],"PurchasableItemId":2182,"SubscriberPrices":[{"CurrencyType":2,"Price":100,"StorefrontSaleData":null,"Type":0}],"Type":0},{"GiftDrop":{"AvatarItemDesc":"","AvatarItemType":0,"ConsumableItemDesc":"iiGTvhOCHkOTNJhb16Zbyw","Context":110000,"Currency":0,"CurrencyType":0,"EquipmentModificationGuid":"","EquipmentPrefabName":"","FriendlyName":"Fancy Bubbly","GiftDropId":2183,"IsQuery":false,"ItemSetFriendlyName":"","ItemSetId":0,"Rarity":50,"SubscribersOnly":false,"Tooltip":" Debug: 2183","Unique":false},"IsFeatured":false,"Prices":[{"CurrencyType":2,"Price":1000,"StorefrontSaleData":null,"Type":0}],"PurchasableItemId":2183,"SubscriberPrices":[{"CurrencyType":2,"Price":1000,"StorefrontSaleData":null,"Type":0}],"Type":0},{"GiftDrop":{"AvatarItemDesc":"","AvatarItemType":0,"ConsumableItemDesc":"EmPvh3I6L0uK_1i8Wy_ylQ","Context":110000,"Currency":0,"CurrencyType":0,"EquipmentModificationGuid":"","EquipmentPrefabName":"","FriendlyName":"Candy Apples","GiftDropId":2184,"IsQuery":false,"ItemSetFriendlyName":"","ItemSetId":0,"Rarity":20,"SubscribersOnly":false,"Tooltip":" Debug: 2184","Unique":false},"IsFeatured":false,"Prices":[{"CurrencyType":2,"Price":95,"StorefrontSaleData":null,"Type":0}],"PurchasableItemId":2184,"SubscriberPrices":[{"CurrencyType":2,"Price":95,"StorefrontSaleData":null,"Type":0}],"Type":0},{"GiftDrop":{"AvatarItemDesc":"","AvatarItemType":0,"ConsumableItemDesc":"Sk_1sm88ZU2zpWn01Lv7hw","Context":110000,"Currency":0,"CurrencyType":0,"EquipmentModificationGuid":"","EquipmentPrefabName":"","FriendlyName":"Celebration Cake","GiftDropId":2185,"IsQuery":false,"ItemSetFriendlyName":"","ItemSetId":0,"Rarity":30,"SubscribersOnly":false,"Tooltip":"Eight slices of tasty cake to share with friends. Debug: 2185","Unique":false},"IsFeatured":false,"Prices":[{"CurrencyType":2,"Price":600,"StorefrontSaleData":null,"Type":0}],"PurchasableItemId":2185,"SubscriberPrices":[{"CurrencyType":2,"Price":600,"StorefrontSaleData":null,"Type":0}],"Type":0},{"GiftDrop":{"AvatarItemDesc":"","AvatarItemType":0,"ConsumableItemDesc":"5hIAZ9wg5EyG1cILf4FS2A","Context":110000,"Currency":0,"CurrencyType":0,"EquipmentModificationGuid":"","EquipmentPrefabName":"","FriendlyName":"Cheese Pizza","GiftDropId":2186,"IsQuery":false,"ItemSetFriendlyName":"","ItemSetId":0,"Rarity":0,"SubscribersOnly":false,"Tooltip":"A cheese pizza to share with friends. Debug: 2186","Unique":false},"IsFeatured":false,"Prices":[{"CurrencyType":2,"Price":85,"StorefrontSaleData":null,"Type":0}],"PurchasableItemId":2186,"SubscriberPrices":[{"CurrencyType":2,"Price":85,"StorefrontSaleData":null,"Type":0}],"Type":0},{"GiftDrop":{"AvatarItemDesc":"","AvatarItemType":0,"ConsumableItemDesc":"BbJb1_0g4EGJP_CeNrjpew","Context":110000,"Currency":0,"CurrencyType":0,"EquipmentModificationGuid":"","EquipmentPrefabName":"","FriendlyName":"Chocolate Cake","GiftDropId":2187,"IsQuery":false,"ItemSetFriendlyName":"","ItemSetId":0,"Rarity":30,"SubscribersOnly":false,"Tooltip":"Eight slices of tasty cake to share with friends. Debug: 2187","Unique":false},"IsFeatured":false,"Prices":[{"CurrencyType":2,"Price":500,"StorefrontSaleData":null,"Type":0}],"PurchasableItemId":2187,"SubscriberPrices":[{"CurrencyType":2,"Price":500,"StorefrontSaleData":null,"Type":0}],"Type":0},{"GiftDrop":{"AvatarItemDesc":"","AvatarItemType":0,"ConsumableItemDesc":"mMCGPgK3tki5S_15q2Z81A","Context":110000,"Currency":0,"CurrencyType":0,"EquipmentModificationGuid":"","EquipmentPrefabName":"","FriendlyName":"Chocolate Donuts","GiftDropId":2188,"IsQuery":false,"ItemSetFriendlyName":"","ItemSetId":0,"Rarity":10,"SubscribersOnly":false,"Tooltip":"A dozen chocolate donuts to share with friends. Debug: 2188","Unique":false},"IsFeatured":false,"Prices":[{"CurrencyType":2,"Price":95,"StorefrontSaleData":null,"Type":0}],"PurchasableItemId":2188,"SubscriberPrices":[{"CurrencyType":2,"Price":95,"StorefrontSaleData":null,"Type":0}],"Type":0},{"GiftDrop":{"AvatarItemDesc":"","AvatarItemType":0,"ConsumableItemDesc":"K9RbXo-4U0q6NbGu8VL1Sw","Context":110000,"Currency":0,"CurrencyType":0,"EquipmentModificationGuid":"","EquipmentPrefabName":"","FriendlyName":"Assorted Chocolates","GiftDropId":2189,"IsQuery":false,"ItemSetFriendlyName":"","ItemSetId":0,"Rarity":30,"SubscribersOnly":false,"Tooltip":"Assorted chocolates to share with friends! Debug: 2189","Unique":false},"IsFeatured":false,"Prices":[{"CurrencyType":2,"Price":250,"StorefrontSaleData":null,"Type":0}],"PurchasableItemId":2189,"SubscriberPrices":[{"CurrencyType":2,"Price":250,"StorefrontSaleData":null,"Type":0}],"Type":0},{"GiftDrop":{"AvatarItemDesc":"","AvatarItemType":0,"ConsumableItemDesc":"7OZ5AE3uuUyqa0P-2W1ptg","Context":110000,"Currency":0,"CurrencyType":0,"EquipmentModificationGuid":"","EquipmentPrefabName":"","FriendlyName":"Glazed Donuts","GiftDropId":2190,"IsQuery":false,"ItemSetFriendlyName":"","ItemSetId":0,"Rarity":0,"SubscribersOnly":false,"Tooltip":"A dozen glazed donuts to share with friends. Debug: 2190","Unique":false},"IsFeatured":false,"Prices":[{"CurrencyType":2,"Price":90,"StorefrontSaleData":null,"Type":0}],"PurchasableItemId":2190,"SubscriberPrices":[{"CurrencyType":2,"Price":90,"StorefrontSaleData":null,"Type":0}],"Type":0}]}""";

    private const int DebugStorefrontIdLow = 2000;
    private const int DebugStorefrontIdHigh = 2099;

    // A purchasable store item, flattened out of a StoreItems entry.
    public sealed class StoreItem
    {
        public int PurchasableItemId;
        public int GiftDropId;
        public int Price;
        public int CurrencyType = 2;
        public string FriendlyName = "";
        // Comma-separated prefab guids for wearable drops, empty for consumables.
        public string AvatarItemDesc = "";
        public string ConsumableItemDesc = "";
        public bool IsConsumable => string.IsNullOrEmpty(AvatarItemDesc) &&
                                   !string.IsNullOrEmpty(ConsumableItemDesc);
    }

    private static List<StoreItem> _index;
    private static readonly object _indexLock = new();

    // PurchasableItemId -> StoreItem, built once from the embedded storefront
    // bodies. buyItem needs this to know the real price and what the purchase
    // grants; without it the request's own RequestedPrice would be trusted blindly.
    public static List<StoreItem> ItemIndex()
    {
        lock (_indexLock)
        {
            if (_index != null) return _index;

            var list = new List<StoreItem>();
            foreach (var body in new[] { ConsumableStorefrontBody, DebugStorefrontTemplate })
                IndexInto(list, body);

            _index = list;
            return _index;
        }
    }

    private static void IndexInto(List<StoreItem> into, string body)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("StoreItems", out var items) ||
                items.ValueKind != System.Text.Json.JsonValueKind.Array) return;

            foreach (var si in items.EnumerateArray())
            {
                var item = new StoreItem
                {
                    PurchasableItemId = Int(si, "PurchasableItemId"),
                    FriendlyName = Str(si, "FriendlyName")
                };

                if (si.TryGetProperty("GiftDrop", out var gd) &&
                    gd.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    item.GiftDropId = Int(gd, "GiftDropId");
                    item.AvatarItemDesc = Str(gd, "AvatarItemDesc");
                    item.ConsumableItemDesc = Str(gd, "ConsumableItemDesc");
                    if (string.IsNullOrEmpty(item.FriendlyName))
                        item.FriendlyName = Str(gd, "FriendlyName");
                }

                if (si.TryGetProperty("Prices", out var prices) &&
                    prices.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var p in prices.EnumerateArray())
                    {
                        if (p.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
                        item.Price = Int(p, "Price");
                        int ct = Int(p, "CurrencyType");
                        if (ct != 0) item.CurrencyType = ct;
                        break;
                    }
                }

                if (item.PurchasableItemId > 0) into.Add(item);
            }
        }
        catch { }
    }

    private static int Int(System.Text.Json.JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.TryGetInt32(out int i) ? i : 0;

    private static string Str(System.Text.Json.JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String
            ? (v.GetString() ?? "")
            : "";

    public static StoreItem FindItem(int purchasableItemId)
    {
        foreach (var it in ItemIndex())
            if (it.PurchasableItemId == purchasableItemId) return it;
        return null;
    }

    public string Response(HttpContext context)
    {
        string path = context.Request.Path.Value ?? "";
        var segs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        int id = 3;
        if (segs.Length > 0 && int.TryParse(segs[^1], out int parsed))
            id = parsed;

        if (id >= DebugStorefrontIdLow && id <= DebugStorefrontIdHigh)
            return DebugStorefrontTemplate.Replace("SF 2000", "SF " + id);

        return ConsumableStorefrontBody;
    }

    public string GetUrl() => "/api/storefronts/v3/giftdropstore/{id}";
    public string GetMethod() => HttpMethods.Get;
}