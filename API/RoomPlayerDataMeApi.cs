namespace Rex_Room_20230414.API;
using System;
using Microsoft.AspNetCore.Http;
using Rex_Room_20230414.Database;

// GET /rooms/{roomId}/playerdata/me - the caller's per-room persistent data
// (name tags, room settings). Reference returns an empty object for a room the
// player has never saved state in.
public class RoomPlayerDataMeApi : IApi
{
    public string Response(HttpContext context) => "{}";

    public string GetUrl() => "/rooms/{roomId}/playerdata/me";
    public string GetMethod() => HttpMethods.Get;
}
