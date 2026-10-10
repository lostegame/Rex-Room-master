namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
public class DefaultApi : IApi
{
    public string Response(HttpContext context)
    {
        var host = context.Request.Host.Value;
        var base_url = $"https://{host}/";
        return $@"{{
            ""Accounts"": ""{base_url}"",
            ""AI"": ""{base_url}"",
            ""API"": ""{base_url}"",
            ""Auth"": ""{base_url}"",
            ""BugReporting"": ""{base_url}"",
            ""Cards"": ""{base_url}"",
            ""CDN"": ""{base_url}"",
            ""Chat"": ""{base_url}"",
            ""Clubs"": ""{base_url}"",
            ""CMS"": ""{base_url}"",
            ""Commerce"": ""{base_url}"",
            ""Data"": ""{base_url}"",
            ""DataCollection"": ""{base_url}"",
            ""Discovery"": ""{base_url}"",
            ""Econ"": ""{base_url}"",
            ""GameLogs"": ""{base_url}"",
            ""Geo"": ""{base_url}"",
            ""Images"": ""{base_url}"",
            ""Leaderboard"": ""{base_url}"",
            ""Link"": ""{base_url}"",
            ""Lists"": ""{base_url}"",
            ""Matchmaking"": ""{base_url}"",
            ""Moderation"": ""{base_url}"",
            ""Notifications"": ""{base_url}"",
            ""PlatformNotifications"": ""{base_url}"",
            ""PlayerSettings"": ""{base_url}"",
            ""RoomComments"": ""{base_url}"",
            ""RoomieIntegrations"": ""{base_url}"",
            ""Rooms"": ""{base_url}"",
            ""Storage"": ""{base_url}"",
            ""Strings"": ""{base_url}"",
            ""StringsCDN"": ""{base_url}"",
            ""Studio"": ""{base_url}"",
            ""Thorn"": ""{base_url}"",
            ""Videos"": ""{base_url}"",
            ""WWW"": ""{base_url}""
        }}";
    }
    public string GetUrl() => "/";
    public string GetMethod() => HttpMethods.Get;
}
