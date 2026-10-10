namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class AnnouncementsSubscriptionUnreadApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/announcements/v2/subscription/mine/unread";
    public string GetMethod() => HttpMethods.Get;
}
