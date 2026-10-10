namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// POST /leaderboard/GetNearbyScores
//
// Reference shape (rex room LogOutput.log):
//   {"Rows":[{"PlayerId":810,"Score":233,"Rank":0},{"PlayerId":983,"Score":218,"Rank":1}, ...]}
// Rank is the zero-based index in the sorted list, so the last row's Rank equals
// Rows.Count-1. The request body carries PlayerId/StatChannel/RoomId/WindowSize.
//
// The client POSTs this route, but the handler used to be registered for GET, so
// it never matched and every request fell through to the generic
// 200 {"Success":true} object, which cannot bind to Rows[] - the scoreboard was
// permanently blank. Register it for POST.
//
// Scores come from each account's real progression data when available, so the
// board reflects actual play instead of hardcoded numbers.
public class LeaderboardNearbyScoresApi : IApi
{
    private readonly string _url;
    public LeaderboardNearbyScoresApi(string url = "/leaderboard/GetNearbyScores") { _url = url; }

    public string Response(HttpContext context)
    {
        int viewer = AccountHelper.GetAccountIdFromToken(context) ?? 0;

        var rows = new List<(int PlayerId, long Score)>();
        foreach (var acc in AccountHelper.LoadAccounts())
        {
            long score = ScoreOf(acc);
            if (score > 0) rows.Add((acc.Id, score));
        }

        // Highest first; the viewer's own row still appears even at 0 so the
        // board is never completely blank for a fresh account.
        rows = rows.OrderByDescending(r => r.Score).ThenBy(r => r.PlayerId).ToList();
        if (!rows.Any(r => r.PlayerId == viewer) && viewer != 0)
            rows.Add((viewer, 0));

        var sb = new StringBuilder("{\"Rows\":[");
        for (int i = 0; i < rows.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append("{\"PlayerId\":").Append(rows[i].PlayerId)
              .Append(",\"Score\":").Append(rows[i].Score)
              .Append(",\"Rank\":").Append(i)
              .Append('}');
        }
        sb.Append("]}");
        return sb.ToString();
    }

    // Accumulated experience is the natural "score" for a room leaderboard.
    private static long ScoreOf(Account acc)
    {
        try { return Math.Max(0, acc.Experience); }
        catch { return 0; }
    }

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Post;
}
