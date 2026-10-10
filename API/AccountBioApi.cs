namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using Rex_Room_20230414.Database;

// GET /account/{accountId}/bio - the short profile bio shown on the player card.
public class AccountBioApi : IApi
{
    public string Response(HttpContext context)
    {
        string path = context.Request.Path.Value ?? "";
        var segs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        int accountId = 0;
        foreach (var s in segs)
            if (int.TryParse(s, out int i)) { accountId = i; break; }

        var acc = AccountHelper.GetAccountById(accountId);
        string bio = acc?.Bio ?? "";
        return $"{{\"accountId\":{accountId},\"bio\":{System.Text.Json.JsonSerializer.Serialize(bio)}}}";
    }

    public string GetUrl() => "/account/{accountId}/bio";
    public string GetMethod() => HttpMethods.Get;
}
