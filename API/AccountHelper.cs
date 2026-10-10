namespace Rex_Room_20230414.API;

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

public static class AccountHelper
{
    public static int? GetAccountIdFromToken(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return null;

        var token = authHeader.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrEmpty(token))
            return null;

        // 1. A JWT, if the caller happens to send one: the account id is the "sub"
        //    claim. Real RecRoom tokens are JWTs.
        var payloadJson = DecodeJwtPayload(token);
        if (!string.IsNullOrEmpty(payloadJson))
        {
            try
            {
                var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);
                if (payload != null && payload.TryGetValue("sub", out var subEl))
                {
                    if (subEl.ValueKind == JsonValueKind.String &&
                        int.TryParse(subEl.GetString(), out int subId))
                        return subId;
                    if (subEl.ValueKind == JsonValueKind.Number &&
                        subEl.TryGetInt32(out int subNum))
                        return subNum;
                }
            }
            catch { }
        }

        // 2. The opaque token this server actually issues. /connect/token,
        //    /account/create and /cachedlogin all hand out a bare GUID that is
        //    stored on the account, not a JWT - so a JWT-only lookup left every
        //    account-scoped route reading as anonymous and answering with empty
        //    lists. Match it against the stored Token, then RefreshToken.
        var byToken = LoadAccounts().FirstOrDefault(a =>
            !string.IsNullOrEmpty(a.Token) &&
            string.Equals(a.Token, token, StringComparison.OrdinalIgnoreCase));
        if (byToken != null) return byToken.Id;

        var byRefresh = LoadAccounts().FirstOrDefault(a =>
            !string.IsNullOrEmpty(a.RefreshToken) &&
            string.Equals(a.RefreshToken, token, StringComparison.OrdinalIgnoreCase));
        if (byRefresh != null) return byRefresh.Id;

        return null;
    }

    private static string DecodeJwtPayload(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2) return "";
        var payload = parts[1];
        payload = payload.Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }
        try
        {
            var bytes = Convert.FromBase64String(payload);
            return Encoding.UTF8.GetString(bytes);
        }
        catch { return ""; }
    }

    public static List<Account> LoadAccounts()
    {
        return Rex_Room_20230414.Database.DatabaseManager.LoadAllPlayerData();
    }

    public static void SaveAccounts(List<Account> accounts)
    {
        foreach (var acc in accounts)
            Rex_Room_20230414.Database.DatabaseManager.SavePlayerData(acc);
    }

    // Persist a single account. GetAccountById returns a fresh object loaded from
    // disk, so mutating it requires an explicit save for the change to stick.
    public static void SaveAccount(Account account)
    {
        if (account == null) return;
        Rex_Room_20230414.Database.DatabaseManager.SavePlayerData(account);
    }

    public static Account GetAccountById(int id)
    {
        return LoadAccounts().FirstOrDefault(a => a.Id == id);
    }

    // Every dorm is titled after its owner: the reference emits
    // "@CosmicPixel3625's Dorm" for the account it is serving. Hardcoding
    // "@Player's Dorm" showed the literal word "Player" instead of the player's
    // own name on the dorm banner.
    public static string DormName(int accountId)
    {
        string name = null;
        try { name = GetAccountById(accountId)?.DisplayName; } catch { }
        if (string.IsNullOrWhiteSpace(name))
        {
            try { name = GetAccountById(accountId)?.Username; } catch { }
        }
        if (string.IsNullOrWhiteSpace(name)) return "@Player's Dorm";
        return "@" + name + "'s Dorm";
    }

    // Same, resolved from the bearer token on the current request.
    public static string DormName(HttpContext context)
    {
        var id = GetAccountIdFromToken(context);
        return DormName(id ?? 0);
    }

    // The real server always emits createdAt as ISO-8601 UTC with exactly three
    // fractional digits and a trailing Z, e.g. "2026-09-03T15:31:21.333Z".
    // Persisted values carry full tick precision and no zone, which some clients
    // refuse to parse, so normalise on the way out.
    public static string IsoUtc(string createdAt)
    {
        if (DateTime.TryParse(createdAt, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal |
                System.Globalization.DateTimeStyles.AssumeUniversal, out var dt))
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture) + "Z";

        return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture) + "Z";
    }

// Reads the raw request body. Buffering is enabled first and the stream is
      // rewound before AND after reading, so this does not steal the body from the
      // next reader: without the rewind, an earlier reader saw an empty stream,
      // which is how buyItem ended up with PurchasableItemId 0 and declined every
      // purchase.
      //
      // The text is CACHED on the request, because rewinding alone was NOT enough
      // to make it repeatable: on a request whose body some earlier component had
      // already touched, the second RawBody call returned "" while the first
      // returned the whole body. Anything that pulls two values out of one body -
      // SettingsSetApi reads "key" AND "value" - silently got the second one as an
      // empty string, so every setting was persisted with an empty value. Caching
      // makes repeated calls return the identical text.
      private const string RawBodyCacheKey = "__rexroom_rawbody";

      public static string RawBody(HttpContext context)
      {
          if (context.Items.TryGetValue(RawBodyCacheKey, out var cached) && cached is string cachedText)
              return cachedText;

          string text = "";
          try
          {
              var body = context.Request.Body;
              if (body != null && body.CanRead)
              {
                  context.Request.EnableBuffering();
                  if (body.CanSeek) body.Position = 0;

                  using (var sr = new System.IO.StreamReader(body, leaveOpen: true))
                      text = sr.ReadToEnd() ?? "";

                  if (body.CanSeek) body.Position = 0;
              }
          }
          catch { text = ""; }

          context.Items[RawBodyCacheKey] = text;
          return text;
      }

    // Pull a single value out of a write body, whatever encoding the client chose.
    //
    // The account writes arrive as application/x-www-form-urlencoded text -
    //   PUT /account/me/bio           body=bio=fffff
    //   PUT /account/me/profileimage  body=imageName=sharecamera%2F...jpg
    // - often WITHOUT a Content-Type header, so Request.HasFormContentType is
    // false and a JSON parse is attempted and fails. The bio then persisted as
    // the literal string "bio=fffff" and the avatar never changed. Detect the
    // "key=value" form directly on the raw body and URL-decode it.
    public static string FormValue(HttpContext context, string key, string altKey = null)
    {
        var raw = RawBody(context).Trim();
        if (raw.Length == 0) return context.Request.Query[key].ToString();

        if (raw[0] == '{')
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.TryGetProperty(key, out var v))
                    return v.ValueKind == System.Text.Json.JsonValueKind.String ? (v.GetString() ?? "") : v.ToString();
                if (altKey != null && root.TryGetProperty(altKey, out var v2))
                    return v2.ValueKind == System.Text.Json.JsonValueKind.String ? (v2.GetString() ?? "") : v2.ToString();
                return "";
            }
            catch { return raw; }
        }

        if (raw.IndexOf('=') > 0)
        {
            foreach (var pair in raw.Split('&'))
            {
                int eq = pair.IndexOf('=');
                if (eq <= 0) continue;
                var k = Uri.UnescapeDataString(pair.Substring(0, eq).Replace("+", " "));
                if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase) ||
                    (altKey != null && string.Equals(k, altKey, StringComparison.OrdinalIgnoreCase)))
                    return Uri.UnescapeDataString(pair.Substring(eq + 1).Replace("+", " "));
            }
            // One pair with a key we did not ask for: the value is still the value.
            if (raw.IndexOf('&') < 0)
                return Uri.UnescapeDataString(raw.Substring(raw.IndexOf('=') + 1).Replace("+", " "));
            return "";
        }

        if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
        {
            try { return System.Text.Json.JsonDocument.Parse(raw).RootElement.GetString() ?? ""; }
            catch { return raw.Trim('"'); }
        }

        return raw;
    }

    public static Account GetAccountByPlatformId(string platformId)
    {
        return LoadAccounts().FirstOrDefault(a => a.PlatformId == platformId);
    }
}
