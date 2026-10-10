namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /api/gameconfigs/v1/all - the full remote game-config key/value list the client
// fetches on boot. Reference returns ~200 entries of live-flag overrides; we serve an
// empty list so the client falls back to its shipped local config (same as
// /api/gameconfigs). No gameplay path reads these keys on our server.
public class GameConfigsV1AllApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/gameconfigs/v1/all";
    public string GetMethod() => HttpMethods.Get;
}
