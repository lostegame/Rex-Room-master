namespace Rex_Room_20230414.API;

using Microsoft.AspNetCore.Http;

public interface IApi
{
    public string Response(HttpContext context);
    public string GetUrl();
    
    public string GetMethod();

    public int GetStatusCode() => 200;
}