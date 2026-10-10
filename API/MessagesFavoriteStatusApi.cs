namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class MessagesFavoriteStatusApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/messages/favoritestatus";
    public string GetMethod() => HttpMethods.Get;
}
