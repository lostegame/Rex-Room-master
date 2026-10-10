namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ClubSearchApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/clubs/search";
    public string GetMethod() => HttpMethods.Get;
}
