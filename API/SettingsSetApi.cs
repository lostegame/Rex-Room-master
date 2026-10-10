namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using Rex_Room_20230414.Database;

public class SettingsSetApi : IApi
{
    // PUT /playersettings stores ONE key/value for the caller and answers empty.
    //
    // It used to silently DISCARD any key matching IsFlowBlockedSettingKey
    // (Recroom.OOBE, TUTORIAL_COMPLETE_MASK, Recroom.AccountCreation.*, ...) and
    // still answer "200 <empty>", so the client believed the write succeeded.
    // Nothing was ever persisted, so the onboarding flags could never stick and
    // the game re-ran the new-user flow on every single login - which is what
    // kept re-seeding the player into rooms other players had just entered.
    //
    // The reference does not filter: /playersettings is a dumb per-player
    // key/value store and "holds every setting the player has"
    // (apps/match/src/test/integration/api.test.ts). Every key the client writes
    // is stored here too.
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 2056;

        // FormValue reads the body whether or not the client set a form
        // Content-Type: these PUTs arrive as "key=..&value=.." and
        // HasFormContentType is often false, which used to fall through to the
        // (empty) query string and store nothing at all.
        string key = AccountHelper.FormValue(context, "key");
        string value = AccountHelper.FormValue(context, "value");
        if (string.IsNullOrEmpty(key)) key = AccountHelper.FormValue(context, "Key");
        if (string.IsNullOrEmpty(value)) value = AccountHelper.FormValue(context, "Value");

        if (!string.IsNullOrEmpty(key))
        {
            DatabaseManager.SaveSetting(accountId, key, value);
            Console.WriteLine($"[SettingsSet] account={accountId} key='{key}' value='{value}'");
        }
        return "";
    }

    public string GetUrl() => "/playersettings";
    public string GetMethod() => HttpMethods.Put;
}