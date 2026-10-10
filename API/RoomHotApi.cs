namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;

// GET /roomserver/rooms/hot?skip=&take=&tag= — {"Results":[room objects], "TotalResults":N}
// Serves the seeded room catalog (Rec Center, Paintball, Lounge, ...).
public class RoomHotApi : IApi
{
    public string Response(HttpContext context)
    {
        var skip = 0;
        var take = 100;
        if (int.TryParse(context.Request.Query["skip"], out var s)) skip = Math.Max(0, s);
        if (int.TryParse(context.Request.Query["take"], out var t)) take = Math.Max(1, t);
        var tag = context.Request.Query["tag"].ToString();
        var viewer = AccountHelper.GetAccountIdFromToken(context);
        return RoomCatalog.PagedJson(RoomCatalog.Hot(tag, skip, take), viewer ?? 0);
    }

    public string GetUrl() => "/roomserver/rooms/hot";
    public string GetMethod() => HttpMethods.Get;
}