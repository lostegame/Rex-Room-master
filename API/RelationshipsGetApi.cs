namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /api/relationships/v2/get — the caller's relationship rows (type 3 = friends).
public class RelationshipsGetApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        return SocialStore.FriendsJson(accountId);
    }

    public string GetUrl() => "/api/relationships/v2/get";
    public string GetMethod() => HttpMethods.Get;
}