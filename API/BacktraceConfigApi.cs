namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class BacktraceConfigApi : IApi
{
    public string Response(HttpContext context) =>
        @"{""ServerAddress"":""https://localhost:5229/"",""ProjectId"":0,""MinidumpUpload"":false,""CaptureScreenshot"":false}";
    public string GetUrl() => "/api/config/v1/backtrace";
    public string GetMethod() => HttpMethods.Get;
}
