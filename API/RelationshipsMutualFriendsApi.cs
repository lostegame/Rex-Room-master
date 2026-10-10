namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class RelationshipsMutualFriendsApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/relationships/mutualfriends";
    public string GetMethod() => HttpMethods.Get;
}
