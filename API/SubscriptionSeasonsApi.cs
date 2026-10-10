namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class SubscriptionSeasonsApi : IApi
{
    public string Response(HttpContext context) => "[]";

    public string GetUrl() => "/api/subscriptionseasons/v1/seasons/current";
    public string GetMethod() => HttpMethods.Get;
}