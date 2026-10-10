namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /api/config/v1/azurespeech
// reference #13: {"Key":"dce8de5b297747d9b5bddcc7f19e8c5b","Region":"eastus","Enabled":false}
// Client DTO EECBPIFHMJF (bool + string + string) => Enabled / Key / Region.
public class AzureSpeechConfigApi : IApi
{
    public string Response(HttpContext context)
    {
        context.Response.ContentType = "text/plain; charset=utf-8";
        return @"{""Key"":"""",""Region"":""eastus"",""Enabled"":false}";
    }

    public string GetUrl() => "/api/config/v1/azurespeech";
    public string GetMethod() => HttpMethods.Get;
}