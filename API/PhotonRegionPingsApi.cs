namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;

public class PhotonRegionPingsApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 2056;
        string body = "";
        try
        {
            using (var reader = new System.IO.StreamReader(context.Request.Body, leaveOpen: true))
                body = reader.ReadToEnd();
        }
        catch { }

        if (string.IsNullOrEmpty(body))
            body = context.Request.QueryString.ToString().TrimStart('?');

        var pings = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                if (body.TrimStart().StartsWith("{"))
                {
                    using var doc = JsonDocument.Parse(body);
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Number &&
                            prop.Value.TryGetInt64(out long ms))
                            pings[prop.Name] = ms;
                    }
                }
                else
                {
                    foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var kv = pair.Split('=', 2);
                        if (kv.Length == 2 && long.TryParse(kv[1], out long ms))
                            pings[Uri.UnescapeDataString(kv[0])] = ms;
                    }
                }
            }
            catch { }
        }

        if (pings.Count > 0)
            PhotonRegionPings.Store(accountId, pings);

        return "";
    }

    public string GetUrl() => "/player/photonregionpings";
    public string GetMethod() => HttpMethods.Put;
}

public static class PhotonRegionPings
{
    private static readonly ConcurrentDictionary<int, (string Region, DateTime At)> _best = new();

    public static void Store(int accountId, Dictionary<string, long> pings)
    {
        string best = "";
        long bestMs = long.MaxValue;
        foreach (var kv in pings)
        {
            if (kv.Value >= 0 && kv.Value < bestMs)
            {
                bestMs = kv.Value;
                best = kv.Key;
            }
        }
        if (string.IsNullOrEmpty(best)) return;
        _best[accountId] = (best, DateTime.UtcNow);
        string log = $"[PhotonRegionPings] account={accountId} count={pings.Count} best={best}@{bestMs}ms regions=[";
        foreach (var kv in pings) log += kv.Key + "=" + kv.Value + " ";
        Console.WriteLine(log + "]");
    }

    public static string BestRegion(int accountId)
    {
        if (_best.TryGetValue(accountId, out var e) && DateTime.UtcNow - e.At < TimeSpan.FromMinutes(10))
            return e.Region;
        return PhotonCloudConfig.Region;
    }
}