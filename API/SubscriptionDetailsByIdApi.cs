namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

// GET /subscription/details/{accountId} — the reference returns
// {"accountId":N,"clubId":C,"subscriberCount":0}; our server previously had
// no route for it (FALLBACK), and the client aborted the room join right
// after this call. The signed-in account is only a Rec Room user (no club).
public class SubscriptionDetailsByIdApi : IApi
{
    public string GetUrl() => "/subscription/details/{accountId}";

    public string GetMethod() => HttpMethods.Get;

    public string Response(HttpContext context)
    {
        var match = Regex.Match(context.Request.Path.Value ?? "", @"/subscription/details/(\d+)$");
        string accountId = match.Success ? match.Groups[1].Value : "0";
        return $@"{{""accountId"":{accountId},""clubId"":0,""subscriberCount"":0}}";
    }
}