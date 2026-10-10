namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;

// Rec Center invention purchase flow. Found from the LIVE client traffic against
// this server (the recflare reference never exercises it, so the shapes are built
// to match the sibling /api/storefronts/v2/buyItem contract):
//
//   GET /api/inventions/v1/details?inventionId=26092
//   GET /api/storefronts/v2/buyInvention?inventionId=26092&requestedPrice=0
//   GET /api/inventions/v1/tagfilters
//
// All three used to be answered with the generic {"Success":true,"Valid":true} -
// an object where the client expects a DTO / list - which is why the client
// re-requested buyInvention four times for the same id and gave up.
//
// The old "buy" answer was a permanent DECLINE (UpdateResponse 303) and the DTO
// carried no ownership signal at all, which together produced the reported
// symptoms: purchases were impossible, and the tile could render as OWNED for an
// invention the player never bought.
//
// Now a purchase is real: it checks the player's token balance, deducts the
// invention's InstantiationCost, grants durable ownership in
// PlayerEconomy.OwnedInventionIds, and answers with the new balance so the UI
// updates. Ownership is reported consistently on every invention DTO, both as
// OwnershipPurchased and IsOwned, so it does not matter which field the client
// happens to bind.
public class InventionsPurchaseApi : IApi
{
    private readonly string _url;
    private readonly string _kind; // details | buy | tagfilters

    // Response codes for BalanceUpdates[].UpdateResponse. The reference returns
    // 303 when it declines; 0 is the success code on the same envelope.
    private const int UpdateSuccess = 0;
    private const int UpdateDeclined = 303;

    private int _status = 200;

    public InventionsPurchaseApi(string url, string kind)
    {
        _url = url;
        _kind = kind;
    }

    public string Response(HttpContext context)
    {
        _status = 200;

        switch (_kind)
        {
            case "tagfilters":
                // A list of tag descriptors, same PascalCase shape the client reads
                // for /api/playerevents/v1/tagfilters. Empty is valid: it just means
                // the invention browser offers no tag filter chips.
                return "[]";

            case "buy":
                return Buy(context);

            default:
                return Details(context);
        }
    }

    private string Details(HttpContext context)
    {
        int id = 0;
        int.TryParse(context.Request.Query["inventionId"], out id);
        var inv = InventionCatalog.Find(id);
        if (inv == null)
        {
            // An unknown id is a real "not found", same as the version route.
            _status = 404;
            return "{\"success\":false,\"error\":{\"message\":\"not found\"}}";
        }

        int viewer = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        bool owned = PlayerEconomy.OwnsInvention(viewer, inv.InventionId);

        // The search DTO plus the cost fields the details pane shows, and the
        // ownership flags driven by the real inventory.
        return InventionCatalog.Dto(inv, viewer) +
               ",\"InstantiationCost\":" + inv.InstantiationCost +
               ",\"LightsCost\":" + inv.LightsCost +
               ",\"ChipsCost\":" + inv.ChipsCost +
               ",\"CloudVariablesCost\":" + inv.CloudVariablesCost +
               ",\"AICost\":" + inv.AICost +
               ",\"OwnershipPurchased\":" + (owned ? "true" : "false") +
               ",\"IsOwned\":" + (owned ? "true" : "false") +
               ",\"IsFavorited\":false" +
               ",\"PurchaseInfo\":" + (owned ? "null" : PurchaseInfoJson(inv, viewer)) +
               "}";
    }

    // What the pane shows to buy with: the cost and the player's balance.
    private static string PurchaseInfoJson(InventionCatalog.Invention inv, int viewer) =>
        "{\"InventionId\":" + inv.InventionId +
        ",\"Price\":" + inv.InstantiationCost +
        ",\"CurrencyType\":2" +
        ",\"Balance\":" + PlayerEconomy.Balance(viewer) +
        ",\"CanAfford\":" +
            (PlayerEconomy.Balance(viewer) >= inv.InstantiationCost ? "true" : "false") +
        "}";

    private string Buy(HttpContext context)
    {
        int inventionId = 0;
        int.TryParse(context.Request.Query["inventionId"], out inventionId);

        int viewer = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        var inv = InventionCatalog.Find(inventionId);

        // Unknown invention -> decline.
        if (inv == null)
            return Envelope(UpdateDeclined, 0, inventionId);

        // Already owned -> report success without charging again. This is what
        // makes the tile show OWNED only after a real purchase.
        if (PlayerEconomy.OwnsInvention(viewer, inventionId))
            return Envelope(UpdateSuccess, PlayerEconomy.Balance(viewer), inventionId);

        int cost = inv.InstantiationCost;
        var result = PlayerEconomy.TrySpend(viewer, cost, out int balance);

        switch (result)
        {
            case PlayerEconomy.SpendResult.Ok:
                PlayerEconomy.GrantInvention(viewer, inventionId);
                Console.WriteLine($"[buyInvention] account={viewer} invention={inventionId} -{cost} balance={balance}");
                return Envelope(UpdateSuccess, balance, inventionId);

            case PlayerEconomy.SpendResult.InsufficientFunds:
                // A genuine "you cannot afford this", not a server error. The
                // client shows the price and leaves the invention unowned.
                Console.WriteLine($"[buyInvention] account={viewer} invention={inventionId} DECLINED insufficient funds (need {cost}, have {balance})");
                return Envelope(UpdateDeclined, balance, inventionId);

            default:
                // No account resolved from the token - decline rather than
                // pretending the purchase worked.
                return Envelope(UpdateDeclined, 0, inventionId);
        }
    }

    // The buyItem/buyInvention envelope: a BalanceUpdates list, the resulting
    // balance, and the invention the update applied to.
    private static string Envelope(int updateResponse, int balance, int inventionId) =>
        "{\"BalanceUpdates\":[{\"UpdateResponse\":" + updateResponse +
        ",\"Data\":[]," +
        // BoughtInventionId / OwnedInventionId mirror the reference's buyItem
        // payload so the client can flip the tile without a refetch.
        "\"InventionId\":" + inventionId +
        ",\"Balance\":" + balance +
        ",\"CurrencyType\":2,\"BalanceType\":-2}]," +
        "\"Balance\":" + balance +
        ",\"CurrencyType\":2,\"BalanceType\":-2" +
        ",\"InventionId\":" + inventionId +
        ",\"ErrorCode\":" + (updateResponse == UpdateSuccess ? 0 : 1) + "}";

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
    public int GetStatusCode() => _status;
}
