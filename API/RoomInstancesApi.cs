namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;

// GET /room/{roomId}/instances — a room's live instances (owner/co-owner only).
// The emulator tracks no live sessions, so this returns the current dorm instance
// for the caller when it's theirs, else an empty list.
public class RoomInstancesApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        if (accountId == 0) return "[]";

        string path = context.Request.Path.Value ?? "";
        var segs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        int roomId = RoomData.RoomId;
        // /room/{roomId}/instances  => roomId is the segment before the last
        if (segs.Length >= 3) int.TryParse(segs[segs.Length - 2], out roomId);

        // Only room creators or co-owners see live instances; the rare non-owner
        // gets an empty list rather than a fork in the join flow.
        if (!RoomCatalog.HasRoom(roomId)) return "[]";
        int creator = RoomCatalog.CreatorOf(roomId);
        if (creator != accountId) return "[]";

        string createdAt = "2021-09-13T10:00:00.000Z";
        // Only the caller's OWN instance is listed, so this stays per-account:
        // reporting the shared global dorm id here would point a second player
        // at the first player's live instance.
        long instanceId = RoomCatalog.IsDorm(roomId)
            ? RoomData.DormInstanceId(accountId)
            : RoomData.RoomInstanceId;
        return $@"[
  {{""roomInstanceId"":{instanceId},""roomId"":{roomId},""subRoomId"":{RoomData.SubRoomId},"
             + $@"""isFull"":false,""createdAt"":""{createdAt}"",""playerIds"":[{accountId}]}}
]";
    }

    public string GetUrl() => "/room/{roomId}/instances";
    public string GetMethod() => HttpMethods.Get;
}