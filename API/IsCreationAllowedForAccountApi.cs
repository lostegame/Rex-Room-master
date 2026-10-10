namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /api/customAvatarItems/v1/isCreationAllowedForAccount — reference #34: {"success":true,"value":null}
public class IsCreationAllowedForAccountApi : IApi
{
    public string Response(HttpContext context) => "{\"success\":true,\"value\":null}";
    public string GetUrl() => "/api/customAvatarItems/v1/isCreationAllowedForAccount";
    public string GetMethod() => HttpMethods.Get;
}