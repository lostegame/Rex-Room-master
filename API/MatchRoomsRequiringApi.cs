namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /match/rooms/requiring/{type} (developer|rrplus) — [].
public class MatchRoomsRequiringApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/match/rooms/requiring/{type}";
    public string GetMethod() => HttpMethods.Get;
}