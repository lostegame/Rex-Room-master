namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class AnnouncementGetApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/announcement/v1/get";
    public string GetMethod() => HttpMethods.Get;
}
