namespace Rex_Room_20230414.API;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

// GET /rooms/bulk?ids=... or ?name=RecCenter - bulk room fetch by ids or by name.
// The client uses the name form to resolve the Rec Center (room 2) before it calls
// POST /matchmake/room/2.
//
// Registered for both /rooms/bulk and /roomserver/rooms/bulk: the client uses the
// roomserver form, and with only the short one registered the generic
// /roomserver/rooms/{roomId} handler answered instead - returning ONE room object
// where the client binds a list.
public class RoomsBulkApi : IApi
{
    private readonly string _url;

    public RoomsBulkApi(string url) { _url = url; }

    public string Response(HttpContext context)
    {
        var viewer = AccountHelper.GetAccountIdFromToken(context) ?? 0;

        // The client sends ?id=2 (singular, repeated) as well as ?ids=1,2.
        var ids = context.Request.Query["ids"].ToString();
        if (string.IsNullOrEmpty(ids))
            ids = string.Join(",", context.Request.Query["id"]);

        if (!string.IsNullOrEmpty(ids))
        {
            var wanted = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => int.TryParse(s.Trim(), out int i) ? (int?)i : null)
                            .Where(i => i.HasValue).Select(i => i!.Value).ToHashSet();
            var byIds = new System.Collections.Generic.List<System.Text.Json.JsonElement>();
            foreach (var id in wanted)
                if (RoomCatalog.TryGetRoom(id, out var r)) byIds.Add(r);
            return RoomCatalog.ListJson(byIds.ToArray(), viewer);
        }

        var name = context.Request.Query["name"].ToString();
        if (!string.IsNullOrEmpty(name))
        {
            if (RoomCatalog.TryGetRoomByName(name, out var single))
                return RoomCatalog.ListJson(new[] { single }, viewer);
            return "[]";
        }

        return "[]";
    }

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
}
