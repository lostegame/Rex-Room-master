namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

public class AccountPhoneApi : IApi
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

        string phone = "";
        if (!string.IsNullOrEmpty(body))
        {
            if (context.Request.HasFormContentType)
            {
                var f = System.Web.HttpUtility.ParseQueryString(body);
                phone = f["phone"] ?? f["number"] ?? "";
            }
            else
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("phone", out var jp)) phone = jp.GetString() ?? "";
                    if (string.IsNullOrEmpty(phone) && root.TryGetProperty("number", out var jn)) phone = jn.GetString() ?? "";
                }
                catch { }
            }
        }

        var accounts = AccountHelper.LoadAccounts();
        var acc = accounts.FirstOrDefault(a => a.Id == accountId.Value);
        if (acc != null)
        {
            acc.Phone = string.IsNullOrEmpty(phone) ? acc.Phone : phone;
            acc.IsNew = false;
            AccountHelper.SaveAccounts(accounts);
            Console.WriteLine($"[AccountPhone] account={accountId.Value} phone='{phone}'");
        }

        return "{\"success\":true,\"error\":\"\"}";
    }

    public string GetUrl() => "/account/me/phone";
    public string GetMethod() => HttpMethods.Post;
}