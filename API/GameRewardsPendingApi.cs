namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class GameRewardsPendingApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context);
        if (accountId == null) return "[]";
        return GiftDrop.DrainPendingJson(accountId.Value);
    }
    public string GetUrl() => "/api/gamerewards/v1/pending";
    public string GetMethod() => HttpMethods.Get;
}
