namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /api/customAvatarItems/v1/isCreationEnabled — reference #35: RESP BODY true (scalar boolean)
public class IsCreationEnabledApi : IApi
{
    public string Response(HttpContext context) => "true";
    public string GetUrl() => "/api/customAvatarItems/v1/isCreationEnabled";
    public string GetMethod() => HttpMethods.Get;
}

// GET /api/customAvatarItems/v1/isRenderingEnabled — reference #36: RESP BODY true (scalar boolean)
public class IsRenderingEnabledApi : IApi
{
    public string Response(HttpContext context) => "true";
    public string GetUrl() => "/api/customAvatarItems/v1/isRenderingEnabled";
    public string GetMethod() => HttpMethods.Get;
}