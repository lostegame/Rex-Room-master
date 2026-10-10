namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /api/storefronts/v4/balance/{currencyId}
//
// This used to answer a hardcoded [{"Balance":10000}] for every player, while
// Account.Tokens was never read or written by anything. The storefront and the
// Rec Center both read this, so it is what the UI shows as the player's token
// count - and it could never agree with what a purchase actually did, because
// purchases could not happen at all.
//
// Now it reports the real balance out of PlayerEconomy.
public class StorefrontBalanceApi : IApi
{
    public string Response(HttpContext context)
    {
        int accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        int balance = PlayerEconomy.Balance(accountId);

        // Same single-element shape as before, now with the real number.
        return "[{\"CurrencyType\":2,\"Platform\":-2,\"Balance\":" + balance + "}]";
    }

    public string GetUrl() => "/api/storefronts/v4/balance/{currencyId}";
    public string GetMethod() => HttpMethods.Get;
}
