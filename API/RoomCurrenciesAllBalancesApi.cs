namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /api/roomcurrencies/v1/getAllBalances
//
// Was a bare [], which meant the room currency HUD had nothing to display at all.
// Now it reports the same real token balance the storefront reports, so the
// in-room counter matches the Rec Center balance and the number that gets spent.
public class RoomCurrenciesAllBalancesApi : IApi
{
    public string Response(HttpContext context)
    {
        int accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        int balance = PlayerEconomy.Balance(accountId);

        return "[{\"CurrencyId\":1,\"CurrencyType\":2,\"Balance\":" + balance +
               ",\"Platform\":-2,\"BalanceType\":-2}]";
    }

    public string GetUrl() => "/api/roomcurrencies/v1/getAllBalances";
    public string GetMethod() => HttpMethods.Get;
}
