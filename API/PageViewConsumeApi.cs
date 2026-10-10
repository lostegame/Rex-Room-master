namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class PageViewConsumeApi : IApi
{
    public string Response(HttpContext context) => "{\"success\":true}";
    public string GetUrl() => "/pageview/consume";
    public string GetMethod() => HttpMethods.Post;
}
