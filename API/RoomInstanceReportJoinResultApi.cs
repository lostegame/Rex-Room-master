namespace Rex_Room_20230414.API;
using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

public class RoomInstanceReportJoinResultApi : IApi
{
    // result=0 == Matchmaking.CPOGLJHDAIC.Success (RecNet.Runtime); any non-zero
    // value (1/2/3/4 observed) is a failure/cancel. The endpoint RECORDS the
    // outcome against the player's CURRENT assignment only (the one /matchmake/*,
    // RoomData.AssignRoom, handed the client and advertised by heartbeat).
    public string Response(HttpContext context)
    {
        int result = -1;
        if (context.Request.HasFormContentType &&
            context.Request.Form.TryGetValue("result", out var v) &&
            int.TryParse(v.ToString(), out var parsed))
            result = parsed;

        var account = AccountHelper.GetAccountIdFromToken(context);
        Console.WriteLine($"[joinresult] result={result} accountId={account?.ToString() ?? "none"}");

        // Only apply the report to the room the player ACTUALLY holds. A stale
        // report for a different instance — an old boot, an earlier cancelled
        // load, or a wrong id — must never tear down the currently advertised
        // assignment: clearing it would make /player/heartbeat fall back to
        // roomInstance:null and push the client's presence out of sync (the
        // "InitialRoomLoad canceled" hang).
        if (account == null ||
            !RoomData.TryGetAssignedInstanceId(context, out long currentInstanceId) ||
            !TryReadInstanceId(context.Request.Path.Value, out long reportedInstanceId) ||
            reportedInstanceId != currentInstanceId)
            return "";

        // result=0 (success): the assignment is already in place — keep it, and
        // mark the assignment as JOINED so presence/heartbeat can start advertising
        // the instance (Fix #1: heartbeat roomInstance stays null until this arrives).
        if (result == 0)
            RoomData.MarkRoomJoined(context);
        // result!=0 (failure/cancel): RECORD ONLY, never clear. The client can
        // send a cancel for the very room it is still loading; clearing here
        // un-advertises it mid-load. Room replacement happens on the next
        // /matchmake/*, and /player/logout clears explicitly.
        return "";
    }

    private static bool TryReadInstanceId(string path, out long instanceId)
    {
        instanceId = 0;
        if (string.IsNullOrEmpty(path)) return false;
        var m = Regex.Match(path, @"/roominstance/(\d+)/reportjoinresult", RegexOptions.IgnoreCase);
        if (!m.Success || !long.TryParse(m.Groups[1].Value, out instanceId)) return false;
        return true;
    }

    public string GetUrl() => "/roominstance/{roomInstanceId}/reportjoinresult";
    public string GetMethod() => HttpMethods.Post;
}