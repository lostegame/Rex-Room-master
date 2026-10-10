namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ClubCategoryTagsApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/club/categoryTags";
    public string GetMethod() => HttpMethods.Get;
}
