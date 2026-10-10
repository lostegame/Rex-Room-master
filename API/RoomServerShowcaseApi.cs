namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class RoomServerShowcaseApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/roomserver/showcase/{roomId}";
    public string GetMethod() => HttpMethods.Get;
}
