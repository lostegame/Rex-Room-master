namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class CustomAvatarItemsFromCreatorApi : IApi
{
    public string Response(HttpContext context) => "{\"Results\":[],\"TotalResults\":0}";
    public string GetUrl() => "/api/customAvatarItems/v2/fromCreator/{accountId}";
    public string GetMethod() => HttpMethods.Get;
}