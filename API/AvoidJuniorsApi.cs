namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class AvoidJuniorsApi : IApi
{
    public string Response(HttpContext context) => "{\"success\":true}";

    public string GetUrl() => "/account/me/avoidjuniors";
    public string GetMethod() => HttpMethods.Put;
}
