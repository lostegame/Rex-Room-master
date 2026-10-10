namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class WalletApi : IApi
{
    public string Response(HttpContext context) =>
        @"[{""Type"":2,""Amount"":10000},{""Type"":0,""Amount"":0}]";

    public string GetUrl() => "/api/currency/v1/balances";
    public string GetMethod() => HttpMethods.Get;
}
