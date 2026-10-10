using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Rex_Room_20230414.API;
using Rex_Room_20230414.Database;

namespace Rex_Room_20230414.API
{
    public class CachedLoginApi : IApi
    {
        private static readonly object _lock = new object();

        public string GetUrl() => "/cachedlogin/forplatformid";
        public string GetMethod() => "GET";

        public string Response(HttpContext context)
        {
            var pathParts = context.Request.Path.ToString().Split('/');
            var platformId = pathParts.Last();
            int platform = pathParts.Length >= 2 && int.TryParse(pathParts[pathParts.Length - 2], out var p) ? p : 0;

            lock (_lock)
            {
                var accounts = AccountHelper.LoadAccounts();
                var existing = accounts
                    .Where(a => a.PlatformId == platformId)
                    .OrderByDescending(a => a.LastLoginTime)
                    .ToList();

                if (existing.Count == 0)
                    return "[]";

                var arr = existing.Select(a => BuildCachedAccountJson(a, platform, platformId)).ToList();
                return "[" + string.Join(",", arr) + "]";
            }
        }

        private static string BuildCachedAccountJson(Account acc, int platform, string platformId)
        {
            var obj = new Dictionary<string, object>
            {
                { "platform", platform },
                { "platformId", platformId },
                { "accountId", acc.Id },
                { "lastLoginTime", acc.LastLoginTime },
                { "requirePassword", false },
                { "registrationStatus", acc.RegistrationStatus },
                { "isNew", acc.IsNew }
            };
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions { PropertyNamingPolicy = null, PropertyNameCaseInsensitive = true });
        }
    }
}