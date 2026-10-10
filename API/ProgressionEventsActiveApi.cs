namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ProgressionEventsActiveApi : IApi
{
    public string Response(HttpContext context) => "-1";
    public string GetUrl() => "/api/progressionEvents/active";
    public string GetMethod() => HttpMethods.Get;
}