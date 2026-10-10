namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class PlayerEventsTagFiltersApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/playerevents/v1/tagfilters";
    public string GetMethod() => HttpMethods.Get;
}
