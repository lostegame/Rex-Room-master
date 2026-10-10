namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class AnnouncementsUnreadApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/announcements/v2/mine/unread";
    public string GetMethod() => HttpMethods.Get;
}
