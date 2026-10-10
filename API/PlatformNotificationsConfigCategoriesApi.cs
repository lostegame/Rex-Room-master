namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /pn/config/categories
//
// The discovery base for PlatformNotifications is https://host/pn, so the client
// requests /pn/config/categories. This handler was registered at the bare
// "/config/categories", which MatchScore never matched against the prefixed path,
// so the request fell through to the generic {"Success":true,"Valid":true} object
// (seen 2x in the live client log). The url is a ctor parameter so both the
// prefixed and bare forms can be registered.
public class PlatformNotificationsConfigCategoriesApi : IApi
{
    private readonly string _url;

    public PlatformNotificationsConfigCategoriesApi(string url = "/pn/config/categories")
    {
        _url = url;
    }

    public string Response(HttpContext context) => "{\"Results\":[{\"CategoryId\":2,\"Importance\":0,\"Name\":\"Friends\",\"Description\":\"Friend requests and friend activity\",\"IsMuteable\":true}],\"TotalResults\":1}";

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
}
