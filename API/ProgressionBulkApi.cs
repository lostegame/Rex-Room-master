namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

public class ProgressionBulkApi : IApi
{
    public string Response(HttpContext context)
    {
        var ids = context.Request.Query["id"];
        return "[" + string.Join(",", ids.Select(id =>
        {
            int parsed = int.TryParse(id, out int i) ? i : 0;
            var acc = AccountHelper.GetAccountById(parsed);
            int level = ProgressionHelper.GetLevel(acc);
            int xp = ProgressionHelper.GetXp(acc);
            return $@"{{""PlayerId"":{parsed},""Level"":{level},""XP"":{xp}}}";
        })) + "]";
    }

    public string GetUrl() => "/api/players/v2/progression/bulk";
    public string GetMethod() => HttpMethods.Get;
}
