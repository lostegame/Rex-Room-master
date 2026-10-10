namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using Rex_Room_20230414.Database;

public class AccountPrivacySettingsApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        return $"{{\"accountId\":{accountId},\"isRecentHistoryVisible\":true}}";
    }
    public string GetUrl() => "/accountprivacysettings/{accountId}";
    public string GetMethod() => HttpMethods.Get;
}