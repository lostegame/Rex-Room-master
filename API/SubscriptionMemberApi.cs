namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class SubscriptionMemberApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/subscription/mine/member";
    public string GetMethod() => HttpMethods.Get;
}
