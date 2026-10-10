namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /api/progressionEvents/event/{eventId} - reference returns 404 when the
// event does not exist (active=-1 means no active event). The client treats the
// 404 as "no event" and proceeds.
public class ProgressionEventsEventApi : IApi
{
    public string Response(HttpContext context) => "{\"success\":false,\"error\":{\"message\":\"not found\"}}";
    public string GetUrl() => "/api/progressionEvents/event/{eventId}";
    public string GetMethod() => HttpMethods.Get;
    public int GetStatusCode() => 404;
}