namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public class ChangeDisplayNameApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context);
        if (accountId == null) return "{\"error\":\"Unauthorized\"}";

        string newDisplayName = "";
        if (context.Request.HasFormContentType)
        {
            var form = context.Request.Form;
            newDisplayName = form.ContainsKey("displayname") ? form["displayname"].ToString() : "";
            if (string.IsNullOrEmpty(newDisplayName)) newDisplayName = form.ContainsKey("displayName") ? form["displayName"].ToString() : "";
            if (string.IsNullOrEmpty(newDisplayName)) newDisplayName = form.ContainsKey("value") ? form["value"].ToString() : "";
        }
        else if (context.Request.ContentType != null && context.Request.ContentType.Contains("json"))
        {
            try
            {
                using var reader = new StreamReader(context.Request.Body);
                var body = reader.ReadToEndAsync().Result ?? "";
                var json = JsonDocument.Parse(string.IsNullOrEmpty(body) ? "{}" : body);
                var root = json.RootElement;
                if (root.TryGetProperty("displayname", out var dn)) newDisplayName = dn.GetString() ?? "";
                if (string.IsNullOrEmpty(newDisplayName) && root.TryGetProperty("displayName", out var dn2)) newDisplayName = dn2.GetString() ?? "";
                if (string.IsNullOrEmpty(newDisplayName) && root.TryGetProperty("value", out var v)) newDisplayName = v.GetString() ?? "";
            }
            catch { }
        }

        var accounts = AccountHelper.LoadAccounts();
        var acc = accounts.FirstOrDefault(a => a.Id == accountId.Value);
        if (acc == null) return "{\"error\":\"AccountNotFound\"}";

        if (!string.IsNullOrEmpty(newDisplayName))
        {
            acc.DisplayName = newDisplayName;
            acc.IsNew = false;
            AccountHelper.SaveAccounts(accounts);
            Console.WriteLine($"[ChangeDisplayName] account={accountId.Value} displayName='{newDisplayName}'");
        }

        return JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "success", true },
            { "error", "" },
            { "value", new Dictionary<string, object>
                {
                    { "accountId", accountId.Value },
                    { "displayName", acc.DisplayName }
                } }
        });
    }

    public string GetUrl() => "/account/me/displayname";
    public string GetMethod() => HttpMethods.Put;
}