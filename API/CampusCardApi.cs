namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class CampusCardApi : IApi
{
    public string Response(HttpContext context) => "{}";
    public string GetUrl() => "/api/CampusCard/v1/UpdateAndGetSubscription";
    public string GetMethod() => HttpMethods.Post;
}
