namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class KeepsakesGlobalConfigApi : IApi
{
    public string Response(HttpContext context) => "{\"KeepsakeFeatureEnabled\":true,\"KeepsakeRoomLimit\":10,\"SocialXpBoostEnabled\":false}";
    public string GetUrl() => "/api/keepsakes/globalconfig";
    public string GetMethod() => HttpMethods.Get;
}