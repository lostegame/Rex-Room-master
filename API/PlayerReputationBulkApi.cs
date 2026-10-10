namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System.Linq;

public class PlayerReputationBulkApi : IApi
{
    public string Response(HttpContext context)
    {
        var ids = context.Request.Query["id"];
        return "[" + string.Join(",", ids.Select(id => $@"{{""AccountId"":{id},""IsCheerful"":true,""Noteriety"":0,""SelectedCheer"":0,""CheerCredit"":20,""CheerGeneral"":0,""CheerHelpful"":0,""CheerCreative"":0,""CheerGreatHost"":0,""CheerSportsman"":0,""SubscriberCount"":0,""SubscribedCount"":0}}")) + "]";
    }

    public string GetUrl() => "/api/playerReputation/v2/bulk";
    public string GetMethod() => HttpMethods.Get;
}
