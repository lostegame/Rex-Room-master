namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class SubscriptionDetailsApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/subscription/mine/details";
    public string GetMethod() => HttpMethods.Get;
}
