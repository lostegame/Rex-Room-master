namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class VoteToKickReasonsApi : IApi
{
    public string Response(HttpContext context) =>
        @"[{""Reason"":""Discriminatory language"",""ReportCategory"":102},
           {""Reason"":""Discriminatory behavior"",""ReportCategory"":102},
           {""Reason"":""Threats or encouraging suicide"",""ReportCategory"":102},
           {""Reason"":""Toxic behavior"",""ReportCategory"":102},
           {""Reason"":""Sexual behavior in public"",""ReportCategory"":101},
           {""Reason"":""Player in walls or floor"",""ReportCategory"":103},
           {""Reason"":""Friendly fire"",""ReportCategory"":103},
           {""Reason"":""Microphone spam"",""ReportCategory"":103},
           {""Reason"":""Inactive in games (AFK)"",""ReportCategory"":6}]";
    public string GetUrl() => "/api/PlayerReporting/v1/voteToKickReasons";
    public string GetMethod() => HttpMethods.Get;
}
