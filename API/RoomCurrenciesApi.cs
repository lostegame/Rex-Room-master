namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class RoomCurrenciesApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/roomcurrencies/v1/currencies";
    public string GetMethod() => HttpMethods.Get;
}
