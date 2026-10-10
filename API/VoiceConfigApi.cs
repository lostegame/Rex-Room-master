namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /mo/voice/config
//
// Reached through the /mo (Moderation) discovery base. Registered at the bare
// "/voice/config" it never matched "/mo/voice/config", so that request returned
// the generic fallback object (seen in the live client log). Empty credentials
// are the right answer - the client reads it as "voice not provisioned".
public class VoiceConfigApi : IApi
{
    private readonly string _url;

    public VoiceConfigApi(string url = "/mo/voice/config")
    {
        _url = url;
    }

    public string Response(HttpContext context) => "{\"AccountId\":\"\",\"ApiKey\":\"\"}";

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
}
