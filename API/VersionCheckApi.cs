namespace Rex_Room_20230414.API;

using Microsoft.AspNetCore.Http;

public class VersionCheckApi : IApi
{
    public string Response(HttpContext context) =>
        @"{
  ""VersionStatus"":0,
  ""UpdateNotificationStage"":0,
  ""IsVersionIslanded"":false,
  ""IsCrossPlayDisabled"":false
}";

    public string GetUrl()
    {
        return "/api/versioncheck/v4";
    }

    public string GetMethod()
    {
        return HttpMethods.Get;
    }
}
