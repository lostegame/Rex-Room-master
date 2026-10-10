namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

public class AccountEmailApi : IApi
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

        string email = "";
        if (!string.IsNullOrEmpty(body))
        {
            if (context.Request.HasFormContentType)
            {
                var f = System.Web.HttpUtility.ParseQueryString(body);
                email = f["email"] ?? "";
            }
            else
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("email", out var je)) email = je.GetString() ?? "";
                }
                catch { }
            }
        }

        var accounts = AccountHelper.LoadAccounts();
        var acc = accounts.FirstOrDefault(a => a.Id == accountId.Value);
        if (acc != null)
        {
            acc.Email = string.IsNullOrEmpty(email) ? acc.Email : email;
            acc.IsNew = false;
            AccountHelper.SaveAccounts(accounts);
            Console.WriteLine($"[AccountEmail] account={accountId.Value} email='{email}'");
        }

        return "{\"success\":true,\"error\":\"\"}";
    }

    public string GetUrl() => "/account/me/email";
    public string GetMethod() => HttpMethods.Post;
}