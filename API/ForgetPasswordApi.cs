namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ForgetPasswordApi : IApi
{
    public string Response(HttpContext context) => "{\"success\":true}";

    public string GetUrl() => "/account/forgotpassword";
    public string GetMethod() => HttpMethods.Post;
}
