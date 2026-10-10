namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class GiftDropStoreApi : IApi
{
    public string Response(HttpContext context) => "[]";

    public string GetUrl() => "/api/giftdrops/v1/store";
    public string GetMethod() => HttpMethods.Get;
}
