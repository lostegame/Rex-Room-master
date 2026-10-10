namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /roomserver/rooms/carousel/rising — {"Results":[room objects], "TotalResults":N}
// Same seeded catalog; the client scrolls this as one feed.
public class RoomCarouselRisingApi : IApi
{
    public string Response(HttpContext context)
    {
        var viewer = AccountHelper.GetAccountIdFromToken(context);
        return RoomCatalog.PagedJson(RoomCatalog.Hot("", 0, 50), viewer ?? 0);
    }

    public string GetUrl() => "/roomserver/rooms/carousel/rising";
    public string GetMethod() => HttpMethods.Get;
}