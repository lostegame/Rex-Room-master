namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;

// GET /api/images/v4/room/{roomId}?sort=1&filter=1&take=100&skip=0
//
// Reference returns an ARRAY of room-image DTOs (Id/Type/TaggedPlayerIds), not a
// paged object - see SavedImageStore for the full shape. Unknown rooms answer []
// rather than falling through to the generic 200 {"Success":true}, which the
// client cannot parse as a list.
public class ImagesRoomApi : IApi
{
    private readonly string _url;
    public ImagesRoomApi(string url = "/api/images/v4/room/{roomId}") { _url = url; }

    public string Response(HttpContext context)
    {
        int roomId = 0;
        var segs = (context.Request.Path.Value ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segs.Length - 1; i++)
            if (string.Equals(segs[i], "room", StringComparison.OrdinalIgnoreCase))
                int.TryParse(segs[i + 1], out roomId);

        var items = SavedImageStore.ByRoom(roomId);

        int skip = 0, take = 100;
        int.TryParse(context.Request.Query["skip"], out skip);
        int.TryParse(context.Request.Query["take"], out take);
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 100);

        var sb = new StringBuilder("[");
        bool first = true;
        foreach (var e in items.Skip(skip).Take(take))
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append(SavedImageStore.RoomImageDto(e));
        }
        sb.Append(']');
        return sb.ToString();
    }

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
}
