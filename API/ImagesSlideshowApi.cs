namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ImagesSlideshowApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/images/slideshow";
    public string GetMethod() => HttpMethods.Get;
}
