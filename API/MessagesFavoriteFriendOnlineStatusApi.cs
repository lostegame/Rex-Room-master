namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class MessagesFavoriteFriendOnlineStatusApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/messages/v1/favoriteFriendOnlineStatus";
    public string GetMethod() => HttpMethods.Get;
}
