namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using Rex_Room_20230414.Database;

public class ParentalControlMeApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        return $"{{\"accountId\":{accountId},\"disallowInAppPurchases\":true}}";
    }
    public string GetUrl() => "/parentalcontrol/me";
    public string GetMethod() => HttpMethods.Get;
}
