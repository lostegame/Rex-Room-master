namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;

public class HubNegotiateApi : IApi
{
    public string Response(HttpContext context)
    {
        string id = Guid.NewGuid().ToString();
        return $@"{{""negotiateVersion"":0,""connectionId"":""{id}"",""connectionToken"":""{id}"",""availableTransports"":[{{""transport"":""WebSockets"",""transferFormats"":[""Text""]}}]}}";
    }

    public string GetUrl() => "/hub/v1/negotiate";
    public string GetMethod() => HttpMethods.Post;
}
