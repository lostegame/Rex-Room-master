namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class EacChallengeApi : IApi
{
    public string Response(HttpContext context) => "\"AA==\"";

    public string GetUrl() => "/eac/challenge";
    public string GetMethod() => HttpMethods.Get;
}
