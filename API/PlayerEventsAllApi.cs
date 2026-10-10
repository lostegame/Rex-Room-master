namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class PlayerEventsAllApi : IApi
{
    public string Response(HttpContext context) => @"{""Created"":[],""Responses"":[]}";
    public string GetUrl() => "/api/playerevents/v1/all";
    public string GetMethod() => HttpMethods.Get;
}
