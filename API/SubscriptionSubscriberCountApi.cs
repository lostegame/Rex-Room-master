namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using Rex_Room_20230414.Database;

public class SubscriptionSubscriberCountApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        return $"{{\"accountId\":{accountId},\"subscriberCount\":0}}";
    }
    public string GetUrl() => "/subscription/subscriberCount/{accountId}";
    public string GetMethod() => HttpMethods.Get;
}
