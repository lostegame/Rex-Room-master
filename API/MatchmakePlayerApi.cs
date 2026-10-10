namespace Rex_Room_20230414.API;
using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

// POST /matchmake/player/{playerId} - "join a friend's room" matchmaking. The
// reference client never calls it in a fresh session, but the button exists in the
// social menu, so it resolves to the same room-instance envelope for the room the
// target player is currently in (falling back to the Rec Center, room 2).
public class MatchmakePlayerApi : IApi
{
    private readonly string _url;

    public MatchmakePlayerApi()
    {
        _url = "/matchmake/player/{playerId}";
    }

    public MatchmakePlayerApi(string url)
    {
        _url = url;
    }

    public string Response(HttpContext context)
    {
        string path = context.Request.Path.Value ?? "";
        var m = Regex.Match(path, @"/player/(\d+)");
        int targetPlayerId = 0;
        if (m.Success) int.TryParse(m.Groups[1].Value, out targetPlayerId);

        int roomId = 2;
        if (targetPlayerId > 0)
        {
            var acc = AccountHelper.GetAccountById(targetPlayerId);
            if (acc != null && RoomCatalog.TryGetRoom(RoomData.RoomId, out _))
                roomId = RoomData.RoomId;
        }

        // Same visit bookkeeping as MatchmakeRoomApi - this is the other join path.
        int visitAccount = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        if (visitAccount != 0) RoomInteractionStore.MarkVisited(visitAccount, roomId);

        var instance = MatchmakeHelper.RoomInstanceJson(roomId, 1, isDorm: false);
        RoomData.AssignRoom(context, instance);
        return MatchmakeHelper.V1Envelope(context, 0, instance);
    }

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Post;
}
