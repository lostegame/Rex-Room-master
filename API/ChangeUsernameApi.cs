namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Rex_Room_20230414.Database;

public class ChangeUsernameApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context);
        if (accountId == null) return "{\"error\":\"Unauthorized\"}";

        string newUsername = "";
        if (context.Request.HasFormContentType)
        {
            var form = context.Request.Form;
            newUsername = form.ContainsKey("username") ? form["username"].ToString() : "";
            if (string.IsNullOrEmpty(newUsername)) newUsername = form.ContainsKey("value") ? form["value"].ToString() : "";
        }
        else if (context.Request.ContentType != null && context.Request.ContentType.Contains("json"))
        {
            try
            {
                using var reader = new StreamReader(context.Request.Body);
                var body = reader.ReadToEndAsync().Result ?? "";
                var json = JsonDocument.Parse(string.IsNullOrEmpty(body) ? "{}" : body);
                var root = json.RootElement;
                if (root.TryGetProperty("username", out var u)) newUsername = u.GetString() ?? "";
                if (string.IsNullOrEmpty(newUsername) && root.TryGetProperty("displayName", out var dn)) newUsername = dn.GetString() ?? "";
                if (string.IsNullOrEmpty(newUsername) && root.TryGetProperty("value", out var v)) newUsername = v.GetString() ?? "";
            }
            catch { }
        }

        var accounts = AccountHelper.LoadAccounts();
        var acc = accounts.FirstOrDefault(a => a.Id == accountId.Value);
        if (acc == null) return "{\"error\":\"AccountNotFound\"}";

        if (string.IsNullOrEmpty(newUsername))
        {
            newUsername = acc.Username ?? acc.DisplayName;
        }
        else
        {
            // Same rule (and same message) the real server enforces: letters and
            // numbers only. Without it the client accepts a name, the rename
            // "succeeds", and the name silently mismatches everywhere else.
            if (!newUsername.All(c => char.IsLetterOrDigit(c)))
                return Failure("Your username can only contain letters and numbers.");

            var taken = new HashSet<string>(DatabaseManager.LoadUsernames(), StringComparer.OrdinalIgnoreCase);
            var otherTaken = accounts.Where(a => a.Id != accountId.Value)
                .Select(a => a.Username).Where(n => !string.IsNullOrEmpty(n));
            foreach (var n in otherTaken) taken.Add(n);
            if (taken.Contains(newUsername))
                return Failure("Username already taken");
        }

        string prevUsername = acc.Username;
        acc.Username = newUsername;
        // Keep displayName tracking the username while the two are still in sync
        // (the "choose your name" step sets both); a later rename must not leave a
        // stale display name showing in the dorm.
        if (string.IsNullOrEmpty(acc.DisplayName) ||
            string.Equals(acc.DisplayName, prevUsername, StringComparison.OrdinalIgnoreCase))
            acc.DisplayName = newUsername;
        acc.IsNew = false;
        acc.RegistrationStatus = 1;
        AccountHelper.SaveAccounts(accounts);
        if (!string.IsNullOrEmpty(prevUsername) && !string.Equals(prevUsername, newUsername, StringComparison.OrdinalIgnoreCase))
            DatabaseManager.RemoveUsername(prevUsername);
        DatabaseManager.RegisterUsername(newUsername);
        Console.WriteLine($"[ChangeUsername] account={accountId.Value} username='{newUsername}'");

        // The client deserialises the FULL account object out of "value". Returning
        // a 3-field stub makes it fail parsing and pop an error even though the
        // rename succeeded, which is what the real server never does.
        return Success(newUsername, new Dictionary<string, object>
        {
            { "accountId", accountId.Value },
            { "username", newUsername },
            { "displayName", acc.DisplayName },
            { "profileImage", acc.ProfileImage ?? "" },
            { "bannerImage", "" },
            { "displayEmoji", acc.DisplayEmoji ?? "" },
            { "isJunior", acc.IsNew ? false : acc.IsJunior },
            { "platforms", 0 },
            { "personalPronouns", acc.PersonalPronouns },
            { "identityFlags", acc.IdentityFlags },
            { "createdAt", AccountHelper.IsoUtc(acc.CreatedAt) }
        });
    }

    private static string Success(string username, object value) =>
        "{\"success\":true,\"error\":\"\",\"value\":" + JsonSerializer.Serialize(value) + "}";

    private static string Failure(string error) =>
        "{\"success\":false,\"error\":" + JsonSerializer.Serialize(error) + ",\"value\":\"\"}";

    public string GetUrl() => "/account/me/username";
    public string GetMethod() => HttpMethods.Put;
}