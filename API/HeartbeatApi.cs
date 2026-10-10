namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class HeartbeatApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 2056;

        // A brand-new account (onboarding not finished) has to be answered with
        // the Orientation instance on EVERY login, not just the create_account
        // call - otherwise the client's first heartbeat reports the dorm and it
        // never runs the new-player flow.
        RoomData.EnsureOrientationForSession(context);

        // A fresh session has no /matchmake/* assignment yet: the client's local
        // presence is its OFFLINE DORM placeholder (instance Int64.MinValue), and
        // its whole-instance presence check (HCMJKBIFBHI / MHIMJEELBKN) treats a
        // null incoming room as in-sync while DIFFERING on any non-null room. So
        // with no assignment the heartbeat answers roomInstance:null (recflare's
        // offline presence shape, not an error). Once a real /matchmake/* assigns a
        // room, this heartbeat echoes it verbatim (see below).
        bool inRoom = RoomData.HasRoomAssigned(context);
        string roomInstance = inRoom ? RoomData.RoomJson(context) : "null";
        string photonFields = inRoom
            ? PhotonSessionInfo.ValueJson(context, accountId)
            : "\"photonAuthToken\":null,\n  \"photonRealtimeAppId\":null,\n  \"photonVoiceAppId\":null,\n  \"photonChatAppId\":null,\n  \"photonRegion\":null,\"photonRoomId\":null,\"voiceConnectionInfo\":null,\"voiceServerId\":null,\"experiments\":null";

        return $@"{{
  ""appVersion"":""20230414"",""deviceClass"":2,""errorCode"":0,""isOnline"":true,""playerId"":{accountId},
  ""roomInstance"":{roomInstance},
  ""statusVisibility"":0,""vrMovementMode"":1,""platform"":0,
  {photonFields}
}}";
    }

    public string GetUrl() => "/player/heartbeat";
    public string GetMethod() => HttpMethods.Post;
}