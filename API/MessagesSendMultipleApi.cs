namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class MessagesSendMultipleApi : IApi
{
    public string Response(HttpContext context) => "{\"Success\":true,\"Valid\":true}";
    public string GetUrl() => "/api/messages/v1/sendMultiple";
    public string GetMethod() => HttpMethods.Post;
}
