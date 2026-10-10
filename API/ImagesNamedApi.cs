namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ImagesNamedApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/images/v2/named";
    public string GetMethod() => HttpMethods.Get;
}
