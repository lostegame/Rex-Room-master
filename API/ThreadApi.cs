namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ThreadApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/thread";
    public string GetMethod() => HttpMethods.Get;
}
