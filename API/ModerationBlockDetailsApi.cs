namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ModerationBlockDetailsApi : IApi
{
    public string Response(HttpContext context) =>
        @"{
  ""ReportCategory"":-1,""Duration"":0,""GameSessionId"":0,""IsHostKick"":false,""Message"":null,""PlayerIdReporter"":null,
  ""IsBan"":false,""IsVoiceModAutoban"":false,""IsDeviceBan"":false,""IsWarning"":false,""VoteKickReason"":null,
  ""TimeoutStartedAt"":null,""AssociatedAccountUsername"":null,""ShowCreatorCodeOfConduct"":false,
  ""TopMessageOverride"":null,""BottomMessageOverride"":null
}";

    public string GetUrl() => "/api/PlayerReporting/v1/moderationBlockDetails";
    public string GetMethod() => HttpMethods.Get;
}
