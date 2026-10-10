namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class AvatarSavedApi : IApi
{
    public string Response(HttpContext context) => "{\"Success\":true,\"Slot\":0}";

    public string GetUrl() => "/api/avatar/v4/saved/set";
    public string GetMethod() => HttpMethods.Post;
}
