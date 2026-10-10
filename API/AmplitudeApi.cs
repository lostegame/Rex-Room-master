namespace Rex_Room_20230414.API;

using Microsoft.AspNetCore.Http;

public class AmplitudeApi : IApi
{
    public string Response(HttpContext context)
    {
        context.Response.ContentType = "text/plain; charset=utf-8";
        return @"{
  ""AmplitudeKey"":"""",
  ""UseRudderStack"":false,
  ""RudderStackKey"":"""",
  ""UseStatSig"":false,
  ""StatSigKey"":"""",
  ""StatSigEnvironment"":0
}";
    }

    public string GetUrl()
    {
        return "/api/config/v1/amplitude";
    }

    public string GetMethod()
    {
        return HttpMethods.Get;
    }
}
