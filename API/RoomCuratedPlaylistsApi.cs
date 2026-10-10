namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class RoomCuratedPlaylistsApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/roomserver/rooms/curated_playlists";
    public string GetMethod() => HttpMethods.Get;
}