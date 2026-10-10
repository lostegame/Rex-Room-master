namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

// POST /cachedlogin/forplatformids — batch lookup of cached logins for a list of platform ids.
// Returns an ARRAY (empty when none), matching the client's expected shape.
public class CachedLoginForPlatformIdsApi : IApi
{
    public string Response(HttpContext context)
    {
        try
        {
            var accounts = AccountHelper.LoadAccounts();
            var requested = new List<string>();

            if (context.Request.HasFormContentType)
            {
                var form = context.Request.Form;
                foreach (var key in form.Keys)
                {
                    foreach (var v in form[key])
                        if (!string.IsNullOrEmpty(v)) requested.Add(v.Trim());
                }
            }
            else if (context.Request.ContentType != null && context.Request.ContentType.Contains("json"))
            {
                using var reader = new StreamReader(context.Request.Body);
                var body = reader.ReadToEndAsync().Result ?? "";
                requested.AddRange(ParsePlatformIds(body));
            }

            var list = new List<object>();
            foreach (var pid in requested)
            {
                var acc = accounts
                    .FirstOrDefault(a => String.Equals(a.PlatformId, pid, StringComparison.Ordinal));
                if (acc != null)
                {
                    list.Add(new Dictionary<string, object>
                    {
                        { "platform", 0 },
                        { "platformId", pid },
                        { "accountId", acc.Id },
                        { "lastLoginTime", acc.LastLoginTime },
                        { "requirePassword", false },
                        { "registrationStatus", acc.RegistrationStatus },
                        { "isNew", acc.IsNew }
                    });
                }
            }

            var response = JsonSerializer.Serialize(list, new JsonSerializerOptions { PropertyNamingPolicy = null });
            Console.WriteLine($"[CachedLoginForPlatformIds] requested={requested.Count} matched={list.Count}");
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CachedLoginForPlatformIds] Error: {ex.Message}");
            return "[]";
        }
    }

    private static List<string> ParsePlatformIds(string body)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(body)) return result;

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in root.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.String)
                    result.Add(el.GetString());
                else if (el.ValueKind == JsonValueKind.Number)
                    result.Add(el.GetRawText());
            }
            return result;
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var key in new[] { "id", "platformIds", "platformids" })
            {
                if (root.TryGetProperty(key, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in prop.EnumerateArray())
                        {
                            if (el.ValueKind == JsonValueKind.String) result.Add(el.GetString());
                            else if (el.ValueKind == JsonValueKind.Number) result.Add(el.GetRawText());
                        }
                    }
                    else if (prop.ValueKind == JsonValueKind.String)
                    {
                        result.Add(prop.GetString());
                    }
                    else if (prop.ValueKind == JsonValueKind.Number)
                    {
                        result.Add(prop.GetRawText());
                    }
                }
            }
        }
        return result;
    }

    public string GetUrl() => "/cachedlogin/forplatformids";
    public string GetMethod() => HttpMethods.Post;
}
