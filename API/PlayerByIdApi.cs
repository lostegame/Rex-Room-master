namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System.Linq;

public class PlayerByIdApi : IApi
{
    public string Response(HttpContext context)
    {
        var ids = context.Request.Query["id"];
        return "[" + string.Join(",", ids.Select(id => $@"{{
  ""appVersion"":""20230414"",""deviceClass"":2,""errorCode"":0,""isOnline"":true,""playerId"":{id},
  ""roomInstance"":{RoomData.RoomJson()},
  ""statusVisibility"":0,""vrMovementMode"":1,""platform"":0,
  ""photonAuthToken"":null,""photonRealtimeAppId"":null,""photonVoiceAppId"":null,""photonChatAppId"":null,
  ""photonRegion"":null,""photonRoomId"":null,""voiceConnectionInfo"":null,""voiceServerId"":null,""experiments"":null
}}")) + "]";
    }

    public string GetUrl() => "/player";
    public string GetMethod() => HttpMethods.Get;
}
