namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

public class ChangePasswordApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context);
        if (accountId == null) return "{\"error\":\"Unauthorized\"}";

        string body = "";
        if (context.Request.Body != null)
        {
            using var reader = new StreamReader(context.Request.Body);
            body = reader.ReadToEndAsync().Result ?? "";
        }

        string newPassword = "";
        if (!string.IsNullOrEmpty(body))
        {
            if (context.Request.HasFormContentType)
            {
                var f = System.Web.HttpUtility.ParseQueryString(body);
                newPassword = f["password"] ?? f["newPassword"] ?? f["value"] ?? "";
            }
            else
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("password", out var jp)) newPassword = jp.GetString() ?? "";
                    if (string.IsNullOrEmpty(newPassword) && root.TryGetProperty("newPassword", out var jnp)) newPassword = jnp.GetString() ?? "";
                    if (string.IsNullOrEmpty(newPassword) && root.TryGetProperty("value", out var jv)) newPassword = jv.GetString() ?? "";
                    if (string.IsNullOrEmpty(newPassword) && root.ValueKind == JsonValueKind.String) newPassword = root.GetString() ?? "";
                }
                catch { newPassword = body.Trim(); }
            }
        }

        var accounts = AccountHelper.LoadAccounts();
        var acc = accounts.FirstOrDefault(a => a.Id == accountId.Value);
        if (acc == null) return "{\"error\":\"AccountNotFound\"}";

        if (!string.IsNullOrEmpty(newPassword))
        {
            acc.Password = newPassword;
            acc.IsNew = false;
            AccountHelper.SaveAccounts(accounts);
            Console.WriteLine($"[ChangePassword] account={accountId.Value} password set ({newPassword.Length} chars)");
        }

        return "{\"success\":true}";
    }

    public string GetUrl() => "/account/me/changepassword";
    public string GetMethod() => HttpMethods.Post;
}