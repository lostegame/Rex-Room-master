namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class GameConfigsApi : IApi
{
    public string Response(HttpContext context) => "[]";

    public string GetUrl() => "/api/gameconfigs";
    public string GetMethod() => HttpMethods.Get;
}
