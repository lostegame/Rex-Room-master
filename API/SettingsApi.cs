namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using Rex_Room_20230414.Database;

public class SettingsApi : IApi
{
    // /playersettings is a DUMB per-player key/value store. It returns every
    // setting the player has, merged over a minimal default set, and strips
    // nothing. This is pinned by the reference server's own integration test
    // (apps/match/src/test/integration/api.test.ts):
    //
    //   // "The map holds every setting the player has, so the write must not
    //   //  replace it."
    //   expect(await stored(3201)).toEqual({
    //     'Recroom.OOBE': '77', TUTORIAL_COMPLETE_MASK: '11', avoidJuniors: 'True',
    //   })
    //
    // This handler used to DELETE Recroom.OOBE / TUTORIAL_COMPLETE_MASK /
    // Recroom.AccountCreation.* on every read whenever
    // account.RegistrationStatus == 0. That was the opposite of the reference,
    // and it is what made the game treat an established player as brand new on
    // every single login:
    //
    //   1. the player finishes onboarding and the client PUTs
    //      "Recroom.AccountCreation.HasStarted=True" (observed live);
    //   2. the very next GET /playersettings stripped it again;
    //   3. the client saw "onboarding not started", re-entered the new-user
    //      flow, and - having no anchor for it - could be placed into whichever
    //      room another player had just entered.
    //
    // The original intent (only ever present in a comment, never in behaviour we
    // want) was to avoid INJECTING a false "onboarding finished" flag. We do not
    // inject one: Defaults below carries only PlayerSessionCount, so an account
    // that has genuinely never onboarded still reads back with no OOBE/TUTORIAL
    // keys and the client shows its start screen on its own.
    private static readonly Dictionary<string, string> Defaults = new Dictionary<string, string>
    {
        { "PlayerSessionCount", "1" }
    };

    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 2056;

        var stored = DatabaseManager.LoadSettings(accountId);

        // Everything the client stored is returned as-is. No key is filtered on
        // the way out - see the reference test above.
        var merged = new Dictionary<string, string>(Defaults);
        foreach (var kv in stored)
            merged[kv.Key] = kv.Value;

        var sb = new StringBuilder();
        sb.Append('[');
        bool first = true;
        foreach (var kv in merged)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append("{");
            sb.AppendFormat("\"PlayerId\":{0},\"Key\":\"{1}\",\"Value\":\"{2}\"",
                accountId, JsonEscape(kv.Key), JsonEscape(kv.Value));
            sb.Append("}");
        }
        sb.Append(']');
        return sb.ToString();
    }

    private static string JsonEscape(string s) =>
        (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

    public string GetUrl() => "/playersettings";
    public string GetMethod() => HttpMethods.Get;
}