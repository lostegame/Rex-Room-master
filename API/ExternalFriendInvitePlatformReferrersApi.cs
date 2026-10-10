namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// POST /api/externalfriendinvite/v1/getplatformreferrers
//
// No external referrers on a private server. Registered for POST because that is
// how the client calls it; with only GET registered the request fell through to
// the generic fallback object instead of binding to the empty list.
public class ExternalFriendInvitePlatformReferrersApi : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/api/externalfriendinvite/v1/getplatformreferrers";
    public string GetMethod() => HttpMethods.Post;
}
