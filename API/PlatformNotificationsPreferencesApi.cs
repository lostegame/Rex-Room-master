namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /pn/preferences
//
// Reached through the /pn discovery base, so the client asks for /pn/preferences.
// It was registered at the bare "/preferences" and fell through to the generic
// fallback body (seen in the live client log). url is a ctor parameter so both
// forms register.
public class PlatformNotificationsPreferencesApi : IApi
{
    private readonly string _url;

    public PlatformNotificationsPreferencesApi(string url = "/pn/preferences")
    {
        _url = url;
    }

    public string Response(HttpContext context) => "{\"MutedCategories\":[]}";

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
}
