namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ResetPasswordApi : IApi
{
    public string Response(HttpContext context) => "{\"success\":true}";

    public string GetUrl() => "/account/resetpassword";
    public string GetMethod() => HttpMethods.Post;
}
