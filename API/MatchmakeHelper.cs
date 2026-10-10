namespace Rex_Room_20230414.API;
using System;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

// Shared matchmaking helpers mirroring the reference match.app:
//  - readCorrelationId: body form field `CorrelationId` (case-insensitive), else
//    query `CorrelationId`/`correlationId`, else the empty correlation id.
//  - roomInstanceJson: the v1 wire roomInstance for a catalog room (same shape as
//    RoomData.RoomJson, but for the resolved room).
//  - Envelope: lowercase v1 `{ errorCode, result, roomInstance, correlationId }`
//    or PascalCase v2 `{ ErrorCode, CorrelationId, RoomInstance, MatchmakingPolicy }`.
public static class MatchmakeHelper
{
    public const string EmptyCorrelationId = "00000000-0000-0000-0000-000000000000";

    public static string ReadCorrelationId(HttpContext context)
    {
        if (context.Request.HasFormContentType &&
            context.Request.Form.TryGetValue("CorrelationId", out var posted) &&
            !string.IsNullOrEmpty(posted))
            return posted.ToString();

        var q = context.Request.Query["CorrelationId"].ToString();
        if (string.IsNullOrEmpty(q)) q = context.Request.Query["correlationId"].ToString();
        return string.IsNullOrEmpty(q) ? EmptyCorrelationId : q;
    }

    // roomInstance object for a specific catalog room. photonRoomId naming from the
    // reference: a matchmade live instance gets a fresh id, but for a standalone
    // emulator server with no co-located Photon the client creates/joins the room
    // itself on our Photon Cloud app, so we hand a stable per-room id the client
    // can create deterministically.
    public static string RoomInstanceJson(int roomId, int subRoomId, bool isDorm, int accountId = 0)
    {
        string location = RoomData.RoomLocation;
        int maxPlayers = isDorm ? 4 : 12;
        int roomInstanceType = isDorm ? (int)RoomData.RoomInstanceType : 0; // 0 = public
        string roomName = isDorm ? AccountHelper.DormName(accountId) : "Room";

        if (RoomCatalog.TryGetRoom(roomId, out var room))
        {
            maxPlayers = room.TryGetProperty("MaxPlayers", out var mp) && mp.TryGetInt32(out var mpi) ? mpi : maxPlayers;
            if (room.TryGetProperty("SubRooms", out var subs) && subs.ValueKind == JsonValueKind.Array)
            {
                foreach (var sub in subs.EnumerateArray())
                {
                    if (sub.TryGetProperty("UnitySceneId", out var s) && s.ValueKind == JsonValueKind.String)
                    {
                        location = s.GetString() ?? location;
                        break;
                    }
                }
            }
            // The catalog's dorm entry carries the placeholder "@Player's Dorm";
            // never let it overwrite the real per-account name.
            if (!isDorm && room.TryGetProperty("Name", out var n) && n.ValueKind == JsonValueKind.String)
                roomName = n.GetString() ?? roomName;
        }

        var region = PhotonCloudConfig.Region;
        return $@"{{
  ""roomInstanceId"":{InstanceIdFor(roomId, subRoomId, isDorm, accountId)},""roomId"":{roomId},""subRoomId"":{subRoomId},""roomInstanceType"":{roomInstanceType},
  ""location"":""{location}"",""dataBlob"":"""",""eventId"":0,""clubId"":0,""roomCode"":"""",
  ""photonRegion"":""{region}"",""photonRegionId"":""{region}"",""photonRoomId"":""{PhotonRoomIdFor(roomId, subRoomId, isDorm, accountId)}"",
  ""name"":""{roomName}"",""maxCapacity"":{maxPlayers},""isFull"":false,""isPrivate"":{(isDorm ? "true" : "false")},""isInProgress"":false,""EncryptVoiceChat"":false
}}";
    }

    // Per-room STABLE ids. The client joins the Photon room named by
    // photonRoomId and then reports the join against roomInstanceId; if either
    // value changed on the next /matchmake/* the client would be sitting in one
    // Photon room while the server advertises another, and the room load hangs
    // (it can never reach "joined", so it never returns to the dorm either).
    //
    // Public rooms get one fixed instance id + Photon name per room (the
    // reference behaves exactly this way). The DORM is per-ACCOUNT instead: a
    // shared dorm instance/Photon room meant every player was dropped into
    // whichever player's dorm already had that room open.
    private static long InstanceIdFor(int roomId, int subRoomId, bool isDorm, int accountId)
    {
        if (isDorm) return RoomData.DormInstanceId(accountId);
        return PublicInstanceId(roomId, subRoomId);
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, long> _publicInstanceIds = new();

    private static long PublicInstanceId(int roomId, int subRoomId)
    {
        string key = roomId + ":" + subRoomId;
        return _publicInstanceIds.GetOrAdd(key, _ => 2000000L + ((long)roomId * 1000L) + subRoomId);
    }

    // One stable Photon room name per room, so re-entering the dorm (or any
    // room) lands the client back in the same Photon room it left. The dorm name
    // is per-ACCOUNT so no two players ever share one Photon world.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _photonRoomIds = new();

    private static string PhotonRoomIdFor(int roomId, int subRoomId, bool isDorm, int accountId)
    {
        if (isDorm) return RoomData.DormPhotonRoomId(accountId);
        string key = roomId + ":" + subRoomId;
        return _photonRoomIds.GetOrAdd(key, _ => Guid.NewGuid().ToString());
    }

    public static string Envelope(HttpContext context, int errorCode, string? roomInstance)
    {
        var correlationId = ReadCorrelationId(context);
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/matchmake/v2/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/matchmake/invite/", StringComparison.OrdinalIgnoreCase))
        {
            return $@"{{""ErrorCode"":{errorCode},""CorrelationId"":""{correlationId}"","
                 + $@"""RoomInstance"":{(roomInstance ?? "null")},""MatchmakingPolicy"":0}}";
        }
        return $@"{{""errorCode"":{errorCode},""result"":{errorCode},""roomInstance"":{(roomInstance ?? "null")},""correlationId"":""{correlationId}""}}";
    }

    public static bool IsV2Path(string path) =>
        path.StartsWith("/matchmake/v2/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/matchmake/invite/", StringComparison.OrdinalIgnoreCase);

    // Lowercase v1 envelope: { errorCode, result, roomInstance, correlationId }.
    public static string V1Envelope(HttpContext context, int errorCode, string? roomInstance)
    {
        var correlationId = ReadCorrelationId(context);
        return $@"{{""errorCode"":{errorCode},""result"":{errorCode},""roomInstance"":{(roomInstance ?? "null")},""correlationId"":""{correlationId}""}}";
    }

    // PascalCase v2 envelope: { ErrorCode, CorrelationId, RoomInstance, MatchmakingPolicy }.
    public static string V2Envelope(HttpContext context, int errorCode, string? roomInstance)
    {
        var correlationId = ReadCorrelationId(context);
        return $@"{{""ErrorCode"":{errorCode},""CorrelationId"":""{correlationId}"","
             + $@"""RoomInstance"":{(roomInstance ?? "null")},""MatchmakingPolicy"":0}}";
    }
}