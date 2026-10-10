namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ConsumablesUnlockedApi : IApi
{
    public string Response(HttpContext context) => "[]";

    public string GetUrl() => "/api/consumables/v2/getUnlocked";
    public string GetMethod() => HttpMethods.Get;
}
