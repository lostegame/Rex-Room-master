namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

public class AccountConfirmPhoneApi : IApi
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

        string code = "", phone = "";
        if (!string.IsNullOrEmpty(body))
        {
            if (context.Request.HasFormContentType)
            {
                var f = System.Web.HttpUtility.ParseQueryString(body);
                code = f["code"] ?? "";
                phone = f["phone"] ?? "";
            }
            else
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("code", out var jc)) code = jc.GetString() ?? "";
                    if (root.TryGetProperty("phone", out var jp)) phone = jp.GetString() ?? "";
                }
                catch { }
            }
        }

        var accounts = AccountHelper.LoadAccounts();
        var acc = accounts.FirstOrDefault(a => a.Id == accountId.Value);
        if (acc != null)
        {
            if (!string.IsNullOrEmpty(phone) && string.IsNullOrEmpty(acc.Phone)) acc.Phone = phone;
            acc.IsNew = false;
            AccountHelper.SaveAccounts(accounts);
            Console.WriteLine($"[AccountConfirmPhone] account={accountId.Value} code='{code}' phone='{phone}' -> accepted");
        }

        return "{\"success\":true,\"error\":\"\"}";
    }

    public string GetUrl() => "/account/me/confirmphone";
    public string GetMethod() => HttpMethods.Post;
}