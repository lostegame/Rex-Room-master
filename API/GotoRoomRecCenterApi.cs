namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// POST /matchmake/reccenter — the Rec Center room (catalog room 2). Kept for the
// client path that enters the social hub by name.
public class GotoRoomRecCenterApi : IApi
{
    public string Response(HttpContext context)
    {
        var instance = MatchmakeHelper.RoomInstanceJson(2, 1, isDorm: false);
        RoomData.AssignRoom(context, instance);
        return MatchmakeHelper.V1Envelope(context, 0, instance);
    }

    public string GetUrl() => "/matchmake/reccenter";
    public string GetMethod() => HttpMethods.Post;
}