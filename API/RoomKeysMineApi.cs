namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class RoomKeysMineApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/roomkeys/v1/mine";
    public string GetMethod() => HttpMethods.Get;
}
