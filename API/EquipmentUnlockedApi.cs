namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class EquipmentUnlockedApi : IApi
{
    public string Response(HttpContext context) => "[]";

    public string GetUrl() => "/api/equipment/v2/getUnlocked";
    public string GetMethod() => HttpMethods.Get;
}
