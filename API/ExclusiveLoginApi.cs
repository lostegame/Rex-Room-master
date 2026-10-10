namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// POST /player/exclusivelogin — reference #46 (session takeover heartbeat lock).
public class ExclusiveLoginApi : IApi
{
    public string Response(HttpContext context) =>
        @"{""errorCode"":0}";

    public string GetUrl() => "/player/exclusivelogin";
    public string GetMethod() => HttpMethods.Post;
}