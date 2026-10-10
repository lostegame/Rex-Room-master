namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class MessagesV3DeleteApi : IApi
{
    public string Response(HttpContext context) => "{\"Success\":true,\"Valid\":true}";
    public string GetUrl() => "/api/messages/v3/delete";
    public string GetMethod() => HttpMethods.Post;
}
