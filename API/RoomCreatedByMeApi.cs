namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /rooms/createdby/me and /roomserver/rooms/createdby/me — rooms created by
// the authed account from the catalog. Reference returns a BARE ARRAY of room
// DTOs; RoomCatalog.ListJson emits exactly that shape.
public class RoomCreatedByMeApi : IApi
{
    private readonly string _url;
    public RoomCreatedByMeApi(string url = "/roomserver/rooms/createdby/me") { _url = url; }

    public string Response(HttpContext context)
    {
        int accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        return RoomCatalog.ListJson(RoomCatalog.OwnedBy(accountId), accountId);
    }

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
}