namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class AvatarSavedGetApi : IApi
{
    public string Response(HttpContext context) => "[]";

    public string GetUrl() => "/api/avatar/v3/saved";
    public string GetMethod() => HttpMethods.Get;
}