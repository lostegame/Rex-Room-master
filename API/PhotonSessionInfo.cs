namespace Rex_Room_20230414.API;

using Microsoft.AspNetCore.Http;
using System;

// Shared builder for the Photon-connection field block handed to the client by
// BOTH GET /player/connection-info and POST /player/heartbeat (when a room is
// assigned). Single source of truth for: minting photonAuthToken
// (PhotonCloudConfig.SignPhotonAuthToken), the three Photon app ids, the pinned
// region, the caller's current Photon room, the assigned Tachyon voice server,
// and the verbatim experiments flags. Previously this block was duplicated in
// PlayerConnectionInfoApi and hard-nulled in HeartbeatApi.
public static class PhotonSessionInfo
{
    public static string ValueJson(HttpContext context, int accountId)
    {
        var account = AccountHelper.GetAccountById(accountId);
        string platformId = account?.PlatformId ?? "";
        long platform = 0;
        long deviceClass = 0;

        // The room the caller currently holds (the one /matchmake/* handed out and
        // heartbeat advertises). Fall back to the requested roomInstanceId, then to
        // the empty string — the client's decoder accepts a missing-value string
        // over null.
        string roomId = RoomData.PhotonRoomIdFor(context,
            context.Request.Query["roomInstanceId"].ToString());

        string photonAuthToken = PhotonCloudConfig.SignPhotonAuthToken(
            accountId, platformId, platform, deviceClass, PhotonCloudConfig.RealtimeAppId);

        var tachyon = TachyonServerFor(accountId);
        string voiceHostPort = tachyon.hostPort;
        string voiceServerId = tachyon.serverId;

        var region = RoomData.RegionFor(context);

        // Returns the FIELDS without enclosing object braces so that endpoints
        // embed them exactly where they belong: heartbeat puts them at the top
        // level of its own object, connection-info wraps them in "value".
        return $@"  ""photonAuthToken"":""{photonAuthToken}"",
  ""photonRealtimeAppId"":""{PhotonCloudConfig.RealtimeAppId}"",
  ""photonVoiceAppId"":""{PhotonCloudConfig.VoiceAppId}"",
  ""photonChatAppId"":""{PhotonCloudConfig.ChatAppId}"",
  ""photonRegion"":""{region}"",
  ""photonRoomId"":""{roomId}"",
  ""voiceConnectionInfo"":""{voiceHostPort}"",
  ""voiceServerId"":""{voiceServerId}"",
  ""experiments"":{PhotonCloudConfig.ExperimentsJson}";
    }

    // tachyonServerFor: pure function of the instance id, round-robin over the
    // configured pool. Empty pool → NO_TACHYON_SERVER (empty fields).
    private static (string hostPort, string serverId) TachyonServerFor(int roomInstanceId)
    {
        var pool = TachyonPool();
        if (pool.Length == 0 || roomInstanceId <= 0) return ("", "");
        return pool[roomInstanceId % pool.Length];
    }

    private static (string hostPort, string serverId)[] TachyonPool()
    {
        var raw = PhotonCloudConfig.TachyonHostPort;
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<(string, string)>();
        var entries = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var list = new System.Collections.Generic.List<(string hostPort, string serverId)>();
        for (int i = 0; i < entries.Length; i++)
            list.Add((entries[i], $"tachyon-{i + 1}"));
        return list.ToArray();
    }
}