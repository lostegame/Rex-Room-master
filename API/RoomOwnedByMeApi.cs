namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;

// GET /rooms/ownedby/{accountId} — also matches /rooms/ownedby/me (accountId = "me").
// Returns the rooms owned/created by that account from the seeded catalog.
//
// The client asks for this on BOTH forms: /roomserver/rooms/ownedby/{id} and
// /rooms/ownedby/{id}. Registering only the roomserver form let the generic
// RoomServerRoomsApi (/rooms/{roomId}) win on the short form - MatchScore does
// not penalize the extra "ownedby" segment - so the client received a single
// room OBJECT where it binds a LIST, which is the
// "HJAELLKNEND: expected:'[', actual:'{'" crash. Hence the url is injected and
// this class is registered twice.
public class RoomOwnedByMeApi : IApi
{
    private readonly string _url;

    public RoomOwnedByMeApi(string url) { _url = url; }

    public string Response(HttpContext context)
    {
        int accountId = 0;
        string path = context.Request.Path.Value ?? "";
        var segs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segs.Length > 0 && string.Equals(segs[segs.Length - 1], "me", StringComparison.OrdinalIgnoreCase))
            accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        else if (segs.Length > 0)
            int.TryParse(segs[segs.Length - 1], out accountId);

        return RoomCatalog.ListJson(RoomCatalog.OwnedBy(accountId), accountId);
    }

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
}