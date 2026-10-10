namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ClubhouseSearchMostActiveNowApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/clubhousesearch/mostactivenow";
    public string GetMethod() => HttpMethods.Get;
}
