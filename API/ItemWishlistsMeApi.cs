namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ItemWishlistsMeApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/itemWishlists/v1/wishlist/me";
    public string GetMethod() => HttpMethods.Get;
}

public class ItemWishlistsByAccountApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/itemWishlists/v1/wishlist/{accountId}";
    public string GetMethod() => HttpMethods.Get;
}