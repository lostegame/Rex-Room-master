namespace Rex_Room_20230414.API;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Rex_Room_20230414.Database;

// GET /club/{clubId} - a single club's public metadata. Shape taken verbatim from the
// reference capture: the client reads ClubId, Name, Joinability, MemberCount and
// CreatorAccountId to render the club card and the join button.
public class ClubByIdApi : IApi
{
    public string Response(HttpContext context)
    {
        string path = context.Request.Path.Value ?? "";
        var segs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        int clubId = 0;
        foreach (var s in segs)
            if (int.TryParse(s, out int i)) { clubId = i; break; }

        var viewer = AccountHelper.GetAccountIdFromToken(context) ?? 0;

        // Clubs are derived from our own accounts: one club per account, named after
        // the player's display name, matching the reference's auto-created club.
        var accs = AccountHelper.LoadAccounts().OrderBy(a => a.Id).ToList();
        if (accs.Count == 0)
            return NotFound();

        var owner = clubId > 0
            ? accs.FirstOrDefault(a => a.Id == clubId)
            : accs.FirstOrDefault(a => a.Id == viewer) ?? accs[0];

        if (owner == null)
            return NotFound();

        string name = !string.IsNullOrEmpty(owner.DisplayName) ? owner.DisplayName : owner.Username;
        return $@"{{
  ""ClubId"":{owner.Id},
  ""Name"":""{Esc(name)}"",
  ""Description"":"""",
  ""Category"":"""",
  ""Visibility"":0,
  ""Joinability"":0,
  ""AllowJuniors"":true,
  ""MainImageName"":""DefaultImgPurple"",
  ""ClubType"":1,
  ""ClubhouseRoomId"":null,
  ""CreatorAccountId"":{owner.Id},
  ""IsRRO"":false,
  ""MinLevel"":0,
  ""State"":0,
  ""MemberCount"":1
}}";
    }

    private static string NotFound() =>
        "{\"success\":false,\"error\":{\"message\":\"not found\"}}";

    private static string Esc(string s) => (s ?? "")
        .Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");

    public string GetUrl() => "/club/{clubId}";
    public string GetMethod() => HttpMethods.Get;
}
