namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /api/keepsakes/rooms/{roomId} - reference responds 204 (no content)
// when the room has no keepsake data. The client's InitialRoomLoad awaits the
// keepsake instance-data download; any 200-with-body here left it waiting on
// "Waiting for Keepsake Instance Data download..." until the room load timed out.
public class KeepsakesRoomApi : IApi
{
    public string Response(HttpContext context) => "";
    public string GetUrl() => "/api/keepsakes/rooms/{roomId}";
    public string GetMethod() => HttpMethods.Get;
    public int GetStatusCode() => 204;
}