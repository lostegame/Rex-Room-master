namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// POST /matchmake/dorm — the caller's dorm instance (offline presence, private
// dormtype). Same lowercove v1 envelope as every matchmake room route.
public class GotoRoomApi : IApi
{
    public string Response(HttpContext context)
    {
        RoomData.AssignRoom(context);

        // Booting into your own dorm is a visit, same as matchmaking into a room.
        int accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        if (accountId != 0) RoomInteractionStore.MarkVisited(accountId, RoomData.RoomId);

        return MatchmakeHelper.V1Envelope(context, 0, RoomData.RoomJson(context));
    }

    public string GetUrl() => "/matchmake/dorm";
    public string GetMethod() => HttpMethods.Post;
}