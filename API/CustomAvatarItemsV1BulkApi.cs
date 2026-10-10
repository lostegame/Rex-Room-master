namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class CustomAvatarItemsV1BulkApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/customAvatarItems/v1/bulk";
    public string GetMethod() => HttpMethods.Get;
}
