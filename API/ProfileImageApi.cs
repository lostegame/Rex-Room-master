namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;

public class ProfileImageApi : IApi
{
    public string Response(HttpContext context) => "{\"Success\":true}";

    public string GetUrl() => "/profileimage";
    public string GetMethod() => HttpMethods.Get;
}
