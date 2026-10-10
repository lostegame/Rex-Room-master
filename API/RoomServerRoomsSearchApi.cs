namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;

// GET /roomserver/rooms?id= or /roomserver/rooms?name= — room lookup by id or name.
// Reference contract (rooms.app.ts §2, 02-rooms-app.md:765-803):
//   - neither `id` nor `name`  -> 400 "Either 'id' or 'name' query parameter is required"
//   - `id` (comma separated, first valid wins) -> that room, or `{}` when there is no match
//   - `name` (case-insensitive)  -> that room, or `{}` when there is no match
// The client deserializes this into a SINGLE room object, so the miss case must be
// `{}` — returning "[]" makes it throw HJAELLKNEND (expected:'{', actual:'[') and
// abort LoadInitialScene with "Unable to connect to offline Orientation game session".
public class RoomServerRoomsSearchApi : IApi
{
    private int _status = 200;

    public string Response(HttpContext context)
    {
        _status = 200;

        var idParam = context.Request.Query["id"].ToString();
        var nameParam = context.Request.Query["name"].ToString();

        if (string.IsNullOrEmpty(idParam) && string.IsNullOrEmpty(nameParam))
        {
            _status = 400;
            return "\"Either 'id' or 'name' query parameter is required\"";
        }

        int viewer = AccountHelper.GetAccountIdFromToken(context) ?? 1;

        if (!string.IsNullOrEmpty(idParam))
        {
            foreach (var part in idParam.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part.Trim(), out int wanted) &&
                    RoomCatalog.TryGetRoom(wanted, out var byId))
                    return RoomCatalog.RoomJson(byId, viewer);
            }
            return "{}";
        }

        if (RoomCatalog.TryGetRoomByName(nameParam, out var byName))
            return RoomCatalog.RoomJson(byName, viewer);

        return "{}"; // no match -> empty room object, NOT an array
    }

    public int GetStatusCode() => _status;

    public string GetUrl() => "/roomserver/rooms";
    public string GetMethod() => HttpMethods.Get;
}
