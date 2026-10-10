namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class PlayerLoginApi : IApi
{
    public string Response(HttpContext context) => "";

    public string GetUrl() => "/player/login";
    public string GetMethod() => HttpMethods.Post;
}
