namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ClubMineCreatedApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/club/mine/created";
    public string GetMethod() => HttpMethods.Get;
}
