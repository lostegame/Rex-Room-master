namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;

// POST /api/players/v1/xp - award XP to the caller. Every level gained enqueues one
// random avatar item as a gift (see ProgressionHelper/GiftDrop), which the client then
// collects from /api/gamerewards/v1/pending. Amount comes from the form body `amount`
// (default 100); `targetPlayerId` may award to another account instead of the caller.
public class PlayersAddXpApi : IApi
{
    public string Response(HttpContext context)
    {
        int amount = 100;
        if (context.Request.HasFormContentType &&
            context.Request.Form.TryGetValue("amount", out var a) &&
            int.TryParse(a.ToString(), out int parsedAmount))
            amount = parsedAmount;

        if (amount <= 0) amount = 100;

        int target = 0;
        if (context.Request.HasFormContentType &&
            context.Request.Form.TryGetValue("targetPlayerId", out var t) &&
            int.TryParse(t.ToString(), out int parsedTarget))
            target = parsedTarget;
        if (target == 0) target = AccountHelper.GetAccountIdFromToken(context) ?? 0;

        var acc = AccountHelper.GetAccountById(target);
        if (acc == null)
            return "{\"success\":false,\"error\":{\"message\":\"account not found\"}}";

        int before = ProgressionHelper.GetLevel(acc);
        int gained = ProgressionHelper.AddXp(acc, amount);
        int after = ProgressionHelper.GetLevel(acc);

        return $"{{\"success\":true,\"PlayerId\":{target},\"XPAdded\":{amount},\"LevelBefore\":{before},\"Level\":{after},\"LevelsGained\":{gained}}}";
    }

    public string GetUrl() => "/api/players/v1/xp";
    public string GetMethod() => HttpMethods.Post;
}
