namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class StatusVisibilityApi : IApi
{
    public string Response(HttpContext context) => "";
    public string GetUrl() => "/player/statusvisibility";
    public string GetMethod() => HttpMethods.Put;
}
