namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class CommunityBoardCurrentApi : IApi
{
    public string Response(HttpContext context) =>
        @"{""FeaturedPlayer"":{""Id"":1,""TitleOverride"":"""",""UrlOverride"":null},
           ""FeaturedRoomGroup"":{""FeaturedRoomGroupId"":2,""Name"":""Featured Rooms"",""Rooms"":[]},
           ""CurrentAnnouncement"":{""Message"":""Welcome to Rec Room!"",""MoreInfoUrl"":""https://www.rec.net""},
           ""InstagramImages"":[],
           ""Videos"":[]}";
    public string GetUrl() => "/api/communityboard/v2/current";
    public string GetMethod() => HttpMethods.Get;
}
