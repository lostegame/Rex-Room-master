namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /featuredrooms/current — the reference only serves this to client build
// 20250718.01 (rooms.app.ts FEATURED_ROOMS_VERSIONS); anything else gets
// c.notFound(). The 2023.4 client we serve is build 20230414, so the correct
// answer for it is 404 — never the fallback 200 {"Success":true}, which the
// client reads as "there is a featured group" and then waits on forever.
// Registered on BOTH prefixes: discovery hands the client .../roomserver.
public class FeaturedRoomsCurrentApi : IApi
{
    private readonly string _url;

    public FeaturedRoomsCurrentApi(string url = "/featuredrooms/current") { _url = url; }

    public string Response(HttpContext context) => "{\"success\":false,\"error\":{\"message\":\"not found\"}}";
    public int GetStatusCode() => 404;
    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
}