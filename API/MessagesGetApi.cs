namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /api/messages (also reached by /api/messages/v2/get via prefix matching) —
// the caller's inbox. The client expects a TOP-LEVEL JSON ARRAY of message
// objects (empty array when the inbox is empty); see SocialStore.MessagesJson.
public class MessagesGetApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        return SocialStore.MessagesJson(accountId);
    }

    public string GetUrl() => "/api/messages";
    public string GetMethod() => HttpMethods.Get;
}