namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
public class ClubHomeMeApi : IApi
{
    public string Response(HttpContext context)
    {
        return @"{
""ClubId"":1,
""Name"":""Dylan's Dorm"",
""Description"":"""",
""Category"":""Social"",
""Visibility"":0,
""Joinability"":0,
""AllowJuniors"":true,
""MainImageName"":""DefaultImgPurple"",
""ClubType"":0,
""ClubhouseRoomId"":7,
""CreatorAccountId"":1,
""IsRRO"":false,
""MinLevel"":0,
""State"":0,
""MemberCount"":1}";
    }
    public string GetUrl() => "/club/home/me";
    public string GetMethod() => HttpMethods.Get;
}
