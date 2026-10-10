namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;

// GET /rooms/visitedby/me and /roomserver/rooms/visitedby/me
//
// Reference returns a BARE ARRAY of room DTOs:
//   /rooms/visitedby/me   -> [{"RoomId":257,"Name":"FlareCenter",...}]
//
// Visits are recorded by RoomInteractionStore's LastVisitedAt read, so the tab
// reflects rooms actually entered instead of always being empty.
public class RoomVisitedByMeApi : IApi
{
    private readonly string _url;
    public RoomVisitedByMeApi(string url = "/roomserver/rooms/visitedby/me") { _url = url; }

    public string Response(HttpContext context)
    {
        int accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        var list = new System.Collections.Generic.List<System.Text.Json.JsonElement>();
        foreach (int roomId in RoomInteractionStore.RoomsVisitedBy(accountId))
            if (RoomCatalog.TryGetRoom(roomId, out var el))
                list.Add(el);
        return RoomCatalog.ListJson(list.ToArray(), accountId);
    }

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
}