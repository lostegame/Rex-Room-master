namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class InventionsFromCreatorsApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/inventions/v1/fromcreators";
    public string GetMethod() => HttpMethods.Get;
}
