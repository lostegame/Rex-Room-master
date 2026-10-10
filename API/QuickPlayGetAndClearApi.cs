namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /api/quickPlay/v1/getandclear — reference #74. Returns the pending
// quick-play invitation for the current player, or all-nulls if none exists.
public class QuickPlayGetAndClearApi : IApi
{
    public string Response(HttpContext context) =>
        @"{""RoomName"":null,""ActionCode"":null,""TargetPlayerId"":null}";

    public string GetUrl() => "/api/quickPlay/v1/getandclear";
    public string GetMethod() => HttpMethods.Get;
}