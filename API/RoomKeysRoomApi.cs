namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class RoomKeysRoomApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/roomkeys/v1/room";
    public string GetMethod() => HttpMethods.Get;
}
