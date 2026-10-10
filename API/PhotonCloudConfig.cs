namespace Rex_Room_20230414.API;

using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Official Photon Cloud wiring for the Rex-Room-20230414 project.
//
// The RecRoom client is handed three Photon application ids in the clear
// (never secrets), the region every session is pinned to, and a short-lived
// HS256 photon auth token minted for the realtime app. Values come from the
// environment first; the defaults are this project's own Photon Cloud apps.
// Enum defaults: region "us" (matches the reference's DEFAULT_PHOTON_REGION).
public static class PhotonCloudConfig
{
    // The user's own Photon Cloud application ids, used verbatim. Handed to the
    // client in the clear.
    public static string RealtimeAppId => VarOr(
        Environment.GetEnvironmentVariable("PHOTON_REALTIME_APP_ID"),
        "d21f86e3-5820-4bad-abc0-f3c268341d38");

    public static string VoiceAppId => VarOr(
        Environment.GetEnvironmentVariable("PHOTON_VOICE_APP_ID"),
        "cfa7506a-16d2-4281-b223-8aa3481c9ccc");

    public static string ChatAppId => VarOr(
        Environment.GetEnvironmentVariable("PHOTON_CHAT_APP_ID"),
        "7b1e5529-7a86-4366-81a6-7a94e6a1e78f");

    // The single region every session is pinned to. Must agree with the region
    // stamped on every room instance or the client cannot connect to the session.
    public static string Region => VarOr(
        Environment.GetEnvironmentVariable("PHOTON_REGION"),
        "us");

    // Comma-separated "host:port" Tachyon voice pool. Empty default: no separate
    // voice server, and connection-info's voice fields stay empty. The client is
    // worse off handed a dead address than told there is no voice server at all.
    public static string TachyonHostPort => VarOr(
        Environment.GetEnvironmentVariable("TACHYON_HOST_PORT"),
        "");

    // The shared HS256 signing key. Same key the account tokens are signed with
    // (ConnectTokenApi), so one secret covers both the account JWT and the
    // photon auth token. Overridable so a deployment can bring its own.
    public static byte[] JwtSecretBytes
    {
        get
        {
            var env = Environment.GetEnvironmentVariable("JWT_SECRET");
            if (!string.IsNullOrEmpty(env)) return Encoding.UTF8.GetBytes(env);
            return new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        }
    }

    public const long TokenTtlSeconds = 86400; // TOKEN_TTL_SECONDS, matches jwt.ts

    private static string VarOr(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    // ---- generatePhotonAuthToken -------------------------------------------
    // Mirrors packages/jwt/src/jwt.ts: HS256 sign with claims
    // { sub, rn.platid, rn.plat, rn.deviceclass, rn.env: 'prod', exp, aud }.
    public static string SignPhotonAuthToken(
        int accountId,
        string platformId,
        long platform,
        long deviceClass,
        string audience)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = new
        {
            sub = accountId.ToString(),
            rn_platid = platformId,          // claim name 'rn.platid'
            rn_plat = platform.ToString(),   // claim name 'rn.plat'
            rn_deviceclass = deviceClass.ToString(), // claim name 'rn.deviceclass'
            rn_env = "prod",                 // claim name 'rn.env'
            exp = now + TokenTtlSeconds,
            aud = audience,
            iat = now,
        };
        var json = JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { PropertyNamingPolicy = null })
            .Replace("\"rn_platid\"", "\"rn.platid\"")
            .Replace("\"rn_plat\"", "\"rn.plat\"")
            .Replace("\"rn_deviceclass\"", "\"rn.deviceclass\"")
            .Replace("\"rn_env\"", "\"rn.env\"");
        var header = "{\"alg\":\"HS256\",\"typ\":\"JWT\"}";
        var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(header));
        var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(json));
        var input = headerB64 + "." + payloadB64;
        using var hmac = new HMACSHA256(JwtSecretBytes);
        var sig = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(input)));
        return input + "." + sig;
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    // ---- PHOTON_EXPERIMENTS ------------------------------------------------
    // Verbatim from the reference server — the client changes how it replicates
    // based on these, so they are not free to tune. The load-bearing one is
    // shouldUseGameServerNetworking=false: true makes the client connect to a
    // local game server (127.0.0.1:7777) instead of Photon.
    public static string ExperimentsJson => "{" +
        "\"networkTransformSyncInterval\":10.0," +
        "\"shouldUseUnreliableOnChange\":false," +
        "\"shouldAvoidDiscontinuityRPCs\":true," +
        "\"shouldAvoidRedundantDiscontinuity\":false," +
        "\"r2RuntimeStaticBaking\":true," +
        "\"r2AutoEmbodiment\":true," +
        "\"r2RuntimeStaticBakingMinShapeThreshold\":1," +
        "\"r2UseCheapReplicas\":true," +
        "\"shouldUseGameServerNetworking\":false}";

    // ---- QOS_REGIONS -------------------------------------------------------
    // Verbatim from the reference. Rec Room's own probe endpoints, served as-is;
    // the ranking the client reports is unused — every session is pinned to Region.
    public static readonly (string Id, string Address)[] QosRegions =
    [
        ("us-west1", "34.169.254.144:50000"),
        ("europe-west1", "35.205.141.119:50000"),
        ("asia-northeast1", "35.200.67.228:50000"),
        ("us-east1", "34.73.244.122:50000"),
        ("us-central1", "34.69.179.51:50000"),
        ("northamerica-northeast1", "34.152.4.100:50000"),
    ];

    public static string QosRegionsJson()
    {
        var sb = new StringBuilder("[");
        for (int i = 0; i < QosRegions.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append("{\"id\":\"").Append(QosRegions[i].Id)
              .Append("\",\"address\":\"").Append(QosRegions[i].Address).Append("\"}");
        }
        sb.Append(']');
        return sb.ToString();
    }
}