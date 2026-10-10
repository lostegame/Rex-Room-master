namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class KeepsakesCategoriesApi : IApi
{
    public string Response(HttpContext context) => "{\"Results\":[],\"TotalResults\":0}";
    public string GetUrl() => "/api/keepsakes/categories";
    public string GetMethod() => HttpMethods.Get;
}