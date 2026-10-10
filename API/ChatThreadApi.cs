namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ChatThreadApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/chat/thread";
    public string GetMethod() => HttpMethods.Get;
}
