namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;

// GET /roomserver/rooms/search?query=&skip=&take= — {"Results":[room objects], "TotalResults":N}
// Searches the seeded room catalog by name.
public class RoomSearchApi : IApi
{
    public string Response(HttpContext context)
    {
        var skip = 0;
        var take = 100;
        if (int.TryParse(context.Request.Query["skip"], out var s)) skip = Math.Max(0, s);
        if (int.TryParse(context.Request.Query["take"], out var t)) take = Math.Max(1, t);
        var name = context.Request.Query["query"].ToString();
        if (string.IsNullOrEmpty(name)) name = context.Request.Query["search"].ToString();
        var viewer = AccountHelper.GetAccountIdFromToken(context);
        return RoomCatalog.PagedJson(RoomCatalog.Search(name, skip, take), viewer ?? 0);
    }

    public string GetUrl() => "/roomserver/rooms/search";
    public string GetMethod() => HttpMethods.Get;
}