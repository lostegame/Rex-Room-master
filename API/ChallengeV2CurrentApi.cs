namespace Rex_Room_20230414.API;
using System;
using Microsoft.AspNetCore.Http;

// GET /api/challenge/v2/getCurrent
// reference #23: object, NOT an array:
// {"ChallengeMapId":...,"CompletedRequired":...,"StartAt":...,"EndAt":...,"ServerTime":...,"Challenges":[...]}
public class ChallengeV2CurrentApi : IApi
{
    public string Response(HttpContext context)
    {
        var now = DateTime.UtcNow;
        int mondayDiff = ((int)now.DayOfWeek + 6) % 7; // Monday=0
        var start = now.Date.AddDays(-mondayDiff);
        var end = start.AddDays(7);
        string fmt = "yyyy-MM-ddTHH:mm:ss.fffffffZ";
        return $@"{{""ChallengeMapId"":1348,""CompletedRequired"":false,""StartAt"":{JsonDate(start, fmt)},""EndAt"":{JsonDate(end, fmt)},""ServerTime"":{JsonDate(now, fmt)},""Challenges"":[]}}";
    }

    private static string JsonDate(DateTime d, string fmt) => "\"" + d.ToString(fmt) + "\"";

    public string GetUrl() => "/api/challenge/v2/getCurrent";
    public string GetMethod() => HttpMethods.Get;
}