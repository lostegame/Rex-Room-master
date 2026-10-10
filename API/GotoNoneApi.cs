namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// POST /matchmake/none — the caller's CURRENT assignment, else their DORM.
// Mirrors the reference handler verbatim (match.app.ts /matchmake/none):
//   const current = presence?.roomInstance ?? (await playerDormInstance(c, id))
//   await enterRoom(c, id, current)
//   return matchmakeResult(c, 0, current)
// It NEVER answers null: the client posts this at startup and while sitting in
// Orientation (the build chooses none rather than dorm on load-in), and a null
// here leaves its whole-instance presence check out-of-sync on the next
// heartbeat — the deterministic boot crash we reproduced against a null answer
// ("Sending matchmaking request to leave room" -> roomInstance:null ->
// presence out-of-sync). A caller already holding a room keeps it (never
// warped); only one with no assignment falls back to their personal dorm.
public class GotoNoneApi : IApi
{
    public string Response(HttpContext context)
    {
        RoomData.EnsureOrientationForSession(context);
        if (!RoomData.HasRoomAssigned(context))
            RoomData.AssignRoom(context);
        return MatchmakeHelper.V1Envelope(context, 0, RoomData.RoomJson(context));
    }

    public string GetUrl() => "/matchmake/none";
    public string GetMethod() => HttpMethods.Post;
}