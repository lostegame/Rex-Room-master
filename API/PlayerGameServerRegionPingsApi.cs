namespace Rex_Room_20230414.API;

using Microsoft.AspNetCore.Http;

// PUT /player/gameserverregionpings — game-server region latency report.
// Accepted and ignored (no-op ack), same as the reference.
public class PlayerGameServerRegionPingsApi : IApi
{
    public string Response(HttpContext context) => "";
    public string GetUrl() => "/player/gameserverregionpings";
    public string GetMethod() => HttpMethods.Put;
}