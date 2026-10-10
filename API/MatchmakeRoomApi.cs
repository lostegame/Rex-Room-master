namespace Rex_Room_20230414.API;
using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

// POST /matchmake/room/{roomId}[/{subRoomId}] - matchmake a public (non-dorm) catalog
// room. Registered in every shape the 2023 client uses:
//   /matchmake/room/{roomId}
//   /matchmake/room/{roomId}/{subRoomId}
//   /matchmake/v2/room/{roomId}          (PascalCase v2 envelope)
//   /matchmake/v2/room/{roomId}/{subRoomId}
// Rec Center is roomId 2; the room is resolved from the RoomCatalog seed so the
// instance carries the room's real name/scene/capacity.
public class MatchmakeRoomApi : IApi
{
    private readonly string _url;
    private readonly bool _hasSubRoom;

    public MatchmakeRoomApi()
    {
        _url = "/matchmake/room/{roomId}";
        _hasSubRoom = false;
    }

    public MatchmakeRoomApi(string url, bool hasSubRoom = false)
    {
        _url = url;
        _hasSubRoom = hasSubRoom;
    }

    public string Response(HttpContext context)
    {
        int roomId = 2;
        int subRoomId = 1;

        string path = context.Request.Path.Value ?? "";
        var m = Regex.Match(path, @"/room/(\d+)(?:/(\d+))?");
        if (m.Success)
        {
            if (int.TryParse(m.Groups[1].Value, out int rid)) roomId = rid;
            if (m.Groups[2].Success && int.TryParse(m.Groups[2].Value, out int sid)) subRoomId = sid;
        }

        if (context.Request.Query.TryGetValue("subRoomId", out var sub) &&
            int.TryParse(sub.ToString(), out int qs)) subRoomId = qs;

        // THIS WAS THE "CAN'T JOIN ANY OTHER ROOM" BUG.
        //
        // The 2023 client always calls the 2-segment form
        // (POST /matchmake/room/2) with no ?subRoomId=, so neither branch above
        // ever fires and subRoomId stays at the hardcoded 1. Only room 1 in
        // Content/ImportRooms.json actually has SubRoomId 1 - for the other 45
        // rooms the server therefore handed back an instance pointing at a
        // subroom that does not exist in the room DTO it served moments later.
        // The client resolved the room, found subRoomId 1 unresolvable, posted
        // reportjoinresult=3 and self-recovered to the dorm. The dorm worked only
        // because its instance subroom id (3565) happens to match its DTO.
        //
        // Resolve the room's REAL first subroom from the catalog. An explicit
        // subRoomId (3rd path segment or query) still wins - this is the fallback.
        if (!m.Groups[2].Success &&
            !context.Request.Query.ContainsKey("subRoomId") &&
            RoomCatalog.TryGetRoom(roomId, out var cat))
        {
            int realSub = RoomCatalog.FirstSubRoomId(cat);
            if (realSub > 0) subRoomId = realSub;
        }

        // Matchmaking is where the client commits to entering the room, so this is
        // the point that counts as a visit for /rooms/visitedby/me.
        int visitAccount = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        if (visitAccount != 0) RoomInteractionStore.MarkVisited(visitAccount, roomId);

        var instance = MatchmakeHelper.RoomInstanceJson(roomId, subRoomId, isDorm: false);
        RoomData.AssignRoom(context, instance);

        // Entering the Dorm pays out the per-entry reward (a 100-token gift box
        // plus a random gift). Other rooms pay nothing.
        if (visitAccount != 0 &&
            RoomData.TryGetAssignedInstanceId(context, out long instanceId) &&
            instanceId > 0)
        {
            JoinReward.Grant(context, visitAccount, (int)instanceId, RoomCatalog.IsDorm(roomId));
        }

        return MatchmakeHelper.V1Envelope(context, 0, instance);
    }

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Post;
}
