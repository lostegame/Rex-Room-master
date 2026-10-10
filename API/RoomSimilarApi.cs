namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;

// GET /roomserver/rooms/{roomId}/similar  (also seen without the roomserver prefix)
// -> a LIST of related rooms.
//
// This route had no handler at all: the generic /roomserver/rooms/{roomId}
// handler answered it instead, so the client received a single room OBJECT
// where it binds a list - the same class of bug as /rooms/ownedby.
public class RoomSimilarApi : IApi
{
    private readonly string _url;

    public RoomSimilarApi(string url) { _url = url; }

    public string Response(HttpContext context)
    {
        int roomId = 0;
        var segs = (context.Request.Path.Value ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries);
        // .../rooms/{roomId}/similar - the id is the segment before "similar".
        for (int i = 0; i < segs.Length - 1; i++)
            if (string.Equals(segs[i + 1], "similar", StringComparison.OrdinalIgnoreCase))
            { int.TryParse(segs[i], out roomId); break; }

        var viewer = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        var take = 6;
        if (int.TryParse(context.Request.Query["take"], out var t) && t > 0) take = t;
        return RoomCatalog.ListJson(RoomCatalog.Similar(roomId, take), viewer);
    }

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
}
