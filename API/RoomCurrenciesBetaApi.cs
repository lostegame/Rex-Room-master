namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class RoomCurrenciesBetaApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/roomcurrencies/beta/v1/currencies";
    public string GetMethod() => HttpMethods.Get;
}
