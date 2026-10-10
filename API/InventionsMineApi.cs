namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class InventionsMineApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/inventions/v2/mine";
    public string GetMethod() => HttpMethods.Get;
}