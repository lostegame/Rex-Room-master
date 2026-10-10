namespace Rex_Room_20230414.API;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

// GET /roomserver/rooms/recommendations - the "Recommended For You" row on the
// home tab. The 2023 client calls it ~34 times per session; without a handler it
// falls into /roomserver/rooms/{roomId} (where "recommendations" is not a number)
// and gets back a single Dorm object, so the row renders one wrong card.
//
// Shape matches the sibling list routes (/rooms/hot, /rooms/carousel/rising):
// a bare JSON array of room DTOs. We recommend the seeded social rooms, the same
// set /rooms/hot exposes, minus the caller's own private dorm.
public class RoomRecommendationsApi : IApi
{
    public string Response(HttpContext context)
    {
        var viewer = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        var rooms = RoomCatalog.Hot();

        var recommended = rooms
            .Where(r => !r.TryGetProperty("IsDorm", out var d) || d.ValueKind != System.Text.Json.JsonValueKind.True)
            .Take(12)
            .ToArray();

        return RoomCatalog.ListJson(recommended, viewer);
    }

    public string GetUrl() => "/roomserver/rooms/recommendations";
    public string GetMethod() => HttpMethods.Get;
}
