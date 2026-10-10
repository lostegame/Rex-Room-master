namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

public class BirthdayApi : IApi
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

        Console.WriteLine($"[DEBUG /account/me/birthday] method={context.Request.Method} body='{body}'");

        int month = 0, day = 0, year = 0;
        string birthdayStr = "";

        if (!string.IsNullOrEmpty(body))
        {
            if (context.Request.HasFormContentType)
            {
                var f = System.Web.HttpUtility.ParseQueryString(body);
                int.TryParse(f["month"], out month);
                int.TryParse(f["day"], out day);
                int.TryParse(f["year"], out year);
                birthdayStr = f["birthday"] ?? "";
            }
            else
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    foreach (var prop in root.EnumerateObject())
                        Console.WriteLine($"[DEBUG /account/me/birthday] JSON field: {prop.Name} = '{prop.Value}'");
                    if (root.TryGetProperty("month", out var jm)) month = jm.GetInt32();
                    if (root.TryGetProperty("day", out var jd)) day = jd.GetInt32();
                    if (root.TryGetProperty("year", out var jy)) year = jy.GetInt32();
                    if (root.TryGetProperty("birthday", out var jb)) birthdayStr = jb.ValueKind == JsonValueKind.Null ? "" : (jb.GetString() ?? "");
                    if (root.TryGetProperty("Birthday", out var jB))
                    {
                        if (jB.TryGetProperty("Month", out var jBm)) month = jBm.GetInt32();
                        if (jB.TryGetProperty("Day", out var jBd)) day = jBd.GetInt32();
                        if (jB.TryGetProperty("Year", out var jBy)) year = jBy.GetInt32();
                    }
                }
                catch { }
            }
        }

        if (month == 0 && day == 0 && year == 0 && string.IsNullOrEmpty(birthdayStr))
        {
            // No data supplied -> nothing to save, just ack.
            Console.WriteLine("[DEBUG /account/me/birthday] No birthday data -> ack only");
            return "{\"success\":true}";
        }

        string iso;
        if (!string.IsNullOrEmpty(birthdayStr))
        {
            iso = birthdayStr;
        }
        else
        {
            iso = $"{year.ToString("D4")}-{month.ToString("D2")}-{day.ToString("D2")}T00:00:00.000Z";
        }

        Console.WriteLine($"[DEBUG /account/me/birthday] resolved birthday iso={iso}");

        var accounts = AccountHelper.LoadAccounts();
        var acc = accounts.FirstOrDefault(a => a.Id == accountId.Value);
        if (acc != null)
        {
            acc.Birthday = iso;

            DateTime parsed;
            bool junior = false;
            if (DateTime.TryParse(iso, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out parsed))
            {
                DateTime now = DateTime.UtcNow;
                int age = now.Year - parsed.Year;
                if (parsed > now.AddYears(-age)) age--;
                junior = age < 13;
            }

            acc.IsJunior = junior;
            acc.IsNew = false;
            AccountHelper.SaveAccounts(accounts);
            Console.WriteLine($"[Birthday] account={accountId.Value} birthday={iso} isJunior={junior}");
        }

        string responseBody = $"{{\"success\":true,\"birthday\":\"{Escape(iso)}\",\"isJunior\":{(!string.IsNullOrEmpty(acc?.Birthday) ? (acc.IsJunior ? "true" : "false") : "null")}}}";
        Console.WriteLine($"[Birthday] SENDING response: {responseBody}");
        return responseBody;
    }

    private static string Escape(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

    public string GetUrl() => "/account/me/birthday";
    public string GetMethod() => HttpMethods.Put;
}