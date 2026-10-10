namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class RoleDeveloperApi : IApi
{
    public string Response(HttpContext context) => "false";
    public string GetUrl() => "/role/developer/{accountId}";
    public string GetMethod() => HttpMethods.Get;
}
