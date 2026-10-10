namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ClubMineMemberApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/club/mine/member";
    public string GetMethod() => HttpMethods.Get;
}
