namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /api/roomconsumables/v1/roomConsumable/room/{roomId}
// Returns the consumables list for a room. A bare fallback JSON causes RecRoom's
// RoomConsumablesManager to log "Unable to fetch consumables for room"; return a
// valid empty array so the room load proceeds.
public class RoomConsumablesApi : IApi
{
    public string Response(HttpContext context) => "[]";

    public string GetUrl() => "/api/roomconsumables/v1/roomConsumable/room/{roomId}";
    public string GetMethod() => HttpMethods.Get;
}
