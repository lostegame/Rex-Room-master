namespace Rex_Room_20230414.API;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

// Shared helpers for the "dorm" room the 2023.4 client enters after login.
// Mirrors the roomInstance object returned by POST /player/heartbeat on RecFlare.
public static class RoomData
{
    // Static dorm defaults; used verbatim by the no-arg RoomJson() and as the
    // base of a per-session dorm assignment. MUST stay the account's stable dorm
    // (room 3070 "@Player's Dorm", subroom 3565, instance 1016400): the reference
    // returns the dorm identically on every entry point and the heartbeat, and the
    // client's whole-instance presence check reads out-of-sync when the emailed
    // instance differs (1020399/3041 was a duplicate dorm seeded 09-28 that failed
    // every session once the server started handing it out).
    public static int RoomId = 3070;
    public static int SubRoomId = 3565;
    public static long RoomInstanceId = 1016400;
    public static int RoomInstanceType = 2;          // 2 = dorm
    public static string RoomLocation = "76d98498-60a1-430c-ab76-b54a29b7a163";
    // Blob id of the room-data save the client downloads after it reads
    // SubRooms[].CurrentSave.DataBlob from /rooms/{id}. The reference dorm body
    // carries "RoomDataBlob" on the subroom AND "DataBlob" on the save; both must
    // resolve, otherwise InitialRoomLoad stalls on the instance-data download.
    public static string RoomDataBlob = "";
    // Monotonic id the reference hands out for each new room-data save.
    public static int SubRoomDataSaveId = 1;
    // The Photon room id this server hands the client. This is OUR OWN room for
    // OUR OWN Photon Cloud apps (PhotonCloudConfig), not RecFlare's: the client
    // creates/joins it on Official Photon Cloud under the configured app id.
    // Same naming rule as the reference: an offline dorm presence (no live
    // session) is the constant "rec.<roomId>"; a matchmade live instance is a
    // fresh id (the per-boot GameId we mint on Photon Cloud).
    public static string PhotonRoomId => PhotonGameCreator.GameId;

    // ---- per-account dorm identity -----------------------------------------
    //
    // EVERY ACCOUNT MUST GET ITS OWN DORM. A dorm is identified to the client by
    // two values: roomInstanceId (what the client reports its join against) and
    // photonRoomId (the actual Photon/Luxon world it connects to). Both used to be
    // single global constants, so although the per-session assignment map was
    // already keyed by account, every account was handed the SAME instance id and
    // the SAME Photon room. The Photon room is the shared world, so a player
    // logging in was literally dropped into whichever player's dorm already had
    // that room open - the "new player is sent into Mohammed's room" bug.
    //
    // Both values must stay STABLE for a given account across requests (the client
    // presence check flags out-of-sync when they change mid-session), so they are
    // derived deterministically from the account id rather than minted randomly.
    // Account 0 (no authenticated caller) keeps the legacy global ids.
    public static long DormInstanceId(int accountId) =>
        accountId <= 0 ? RoomInstanceId : RoomInstanceId + ((long)accountId * 1000L);

    public static string DormPhotonRoomId(int accountId) =>
        accountId <= 0 ? PhotonRoomId : PhotonRoomId + "-dorm-" + accountId;

    // NO boot-time room for accounts that already hold one. A session holds NO
    // assignment until a real /matchmake/* (the PLAY flow: /matchmake/dorm,
    // /matchmake/room/{id}, /quickplay) hands one out - see /player/heartbeat and
    // /matchmake/none, which reflect the actual state.

    // ---- Orientation (the brand-new-account flow) ---------------------------
    //
    // Reference, recflare-server apps/auth/src/auth.app.ts:
    //   const ORIENTATION_ROOM_ID = 13
    //   const ORIENTATION_INSTANCE_ID = -2
    //     "The client loads Orientation locally (no matchmake) and tags its
    //      instance with the sentinel id -2. The heartbeat must echo that exact
    //      roomInstanceId or the client treats presence as out-of-sync and bounces
    //      the player to the dorm."
    //   placeNewPlayerInOrientation() runs at account creation and writes that
    //   instance into the shared presence table: roomInstanceType Public,
    //   isPrivate false, photonRoomId `rec.13`, name `^<room name>`, and the
    //   SubRoom's real UnitySceneId (an empty location makes the client reject the
    //   session with "unknown scene location ID").
    //
    // The instance fields below are the reference shape filled from the LOCAL room
    // catalog (Content/ImportRooms.json room 13: SubRoomId 23, MaxPlayers 1,
    // UnitySceneId c79709d8-a31b-48aa-9eb8-cc31ba9505e8), so nothing is invented.
    //
    // This seeding used to be absent on the grounds that the 2023.4 client boots
    // its local presence into an OFFLINE DORM placeholder (instance Int64.MinValue,
    // bit63 set) rather than -2, so echoing -2 produced a "local presence is
    // out-of-sync" re-broadcast storm. That is a client-side observation, not a
    // protocol fact, so the seed is kept strictly to accounts that hold NO
    // assignment yet, and PlayerLogoutApi keeps the spurious post-creation logout
    // from wiping it.
    public const int OrientationRoomId = 13;
    public const long OrientationInstanceId = -2;
    private const int OrientationSubRoomId = 23;
    private const string OrientationLocation = "c79709d8-a31b-48aa-9eb8-cc31ba9505e8";
    private const int OrientationMaxPlayers = 1;

    public static string OrientationRoomJson() => $@"{{
  ""roomInstanceId"":{OrientationInstanceId},""roomId"":{OrientationRoomId},""subRoomId"":{OrientationSubRoomId},""roomInstanceType"":0,
  ""location"":""{OrientationLocation}"",""dataBlob"":"""",""eventId"":0,""clubId"":0,""roomCode"":"""",
  ""photonRegion"":""us"",""photonRegionId"":""us"",""photonRoomId"":""rec.{OrientationRoomId}"",
  ""name"":""^Orientation"",""maxCapacity"":{OrientationMaxPlayers},""isFull"":false,""isPrivate"":false,""isInProgress"":false,""EncryptVoiceChat"":false
}}";

    // Seed a brand-new account into Orientation. The assignment map is keyed by
    // account (see SessionKey), so the seed survives until the client's first real
    // /matchmake/* replaces it, and it is never shared between players.
    // A real assignment is never clobbered, so this can only ever fire for an
    // account that has not been placed anywhere yet.
    public static void AssignOrientation(int accountId)
    {
        if (accountId <= 0) return;
        string key = "account:" + accountId;

        // Gate on PERSISTED onboarding state, not on the in-memory assignment.
        // _roomAssigned lives for the whole server process, so a player who
        // logged in once (landing in their dorm) still has an entry when the
        // new-user flow runs again in that same process. The old
        // ContainsKey guard therefore turned the seed into a no-op for exactly
        // the accounts that needed it, and the heartbeat kept advertising the
        // stale dorm - so the client never entered Orientation and repeated the
        // setup steps forever. The client sets HasFinished=True when onboarding
        // completes, and that survives restarts, so it is the reliable gate.
        try
        {
            var settings = Rex_Room_20230414.Database.DatabaseManager.LoadSettings(accountId);
            if (settings.TryGetValue("Recroom.AccountCreation.HasFinished", out var finished) &&
                string.Equals(finished, "True", StringComparison.OrdinalIgnoreCase))
                return;
        }
        catch { }

        // A stale non-Orientation assignment must not block a genuinely new
        // player, so this deliberately overwrites whatever was there before.
        var parsed = ParseRoomJson(OrientationRoomJson());
        if (parsed == null) return;
        bool had = _roomAssigned.ContainsKey(key);
        _roomAssigned[key] = parsed;
        Console.WriteLine($"[Orientation] seeded account={accountId} instance={OrientationInstanceId} " +
                          $"room={OrientationRoomId} subRoom={OrientationSubRoomId} photon=rec.{OrientationRoomId}" +
                          (had ? " (replaced a stale assignment)" : ""));
    }

    // Re-apply the Orientation seed on every presence read for an account that has
    // NOT finished onboarding.
    //
    // The reference writes the Orientation instance into the SHARED presence
    // table at account creation, so heartbeat keeps echoing it on every login
    // until the player matchmakes away. Our assignment map is in-memory, and the
    // create_account seed only fires the very first time an account is made, so
    // both of these silently dropped a brand-new player into their dorm:
    //   - a server restart (the map starts empty and nothing re-seeds), and
    //   - any login that is not create_account (the client re-entering through
    //     cached_login / refresh_token, which is what it actually does).
    // A real /matchmake/* always wins, so an established player is never moved.
    public static void EnsureOrientationForSession(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context);
        if (!accountId.HasValue || accountId.Value <= 0) return;
        string key = "account:" + accountId.Value;
        if (_roomAssigned.ContainsKey(key)) return;
        AssignOrientation(accountId.Value);
    }

    public static bool IsOrientationAssigned(HttpContext context) =>
        TryGetAssignedInstanceId(context, out long instanceId) && instanceId == OrientationInstanceId;

    // Per-session room assignment state: the FULL room the player was handed by
    // /matchmake/* (not just a boolean flag). /player/heartbeat advertises EXACTLY
    // this room — its verbatim JSON plus its photon fields — so the client's
    // presence stays in sync with what it was told. One entry per session key.
    private sealed class AssignedRoom
    {
        public long RoomInstanceId = RoomData.RoomInstanceId;
        public int RoomId = RoomData.RoomId;
        public int SubRoomId = RoomData.SubRoomId;
        public int RoomInstanceType = RoomData.RoomInstanceType;
        public string RoomLocation = RoomData.RoomLocation;
        public string RoomName = "";
        public bool IsPrivate = true;
        public int MaxCapacity = 4;
        public string Region = PhotonCloudConfig.Region;
        public string PhotonRoomId = "";
        public string RoomJson = "";    // verbatim instance JSON the client was shown
        // True only once the client has confirmed the join for THIS assignment via
        // POST /roominstance/{id}/reportjoinresult result=0. Until then the player
        // is still "joining" and presence must not yet advertise the instance.
        public bool Joined;
    }

    private static readonly ConcurrentDictionary<string, AssignedRoom> _roomAssigned = new();
    private static readonly ConcurrentDictionary<string, int> _loginLockToAccount = new();
    private static readonly ConcurrentDictionary<int, string> _accountToLoginLock = new();

      private static string SessionKey(HttpContext context)
      {
          var accountId = AccountHelper.GetAccountIdFromToken(context);

          string loginLock = null;
          // Reading Request.Form consumes Request.Body. A handler that already
          // buffered the body (e.g. ImagesUploadSavedApi reading the multipart
          // upload) would then hit "Unexpected end of Stream" and 500. EnableBuffering
          // rewinds so the body can be read again; guard in a try/catch because a
          // genuinely non-form request must not throw here.
          try
          {
              if (context.Request.HasFormContentType && context.Request.Body != null)
              {
                  if (!context.Request.Body.CanSeek)
                      context.Request.EnableBuffering();
                  if (context.Request.Form.TryGetValue("LoginLock", out var ll) &&
                      !string.IsNullOrEmpty(ll))
                      loginLock = ll.ToString();
              }
          }
          catch { }

        if (loginLock != null && accountId.HasValue)
        {
            _loginLockToAccount[loginLock] = accountId.Value;
            _accountToLoginLock[accountId.Value] = loginLock;
        }

        // KEY: the room assignment belongs to the ACCOUNT (Bearer `sub`), which is
        // stable across logout->relogin, whereas LoginLock ROTATES whenever the
        // client starts a new login session (observed live: matchmake/dorm under
        // lock X1, then connect/token + player/login under a NEW lock X2, then a
        // heartbeat reading null). Keying by LoginLock stranded the dorm assignment
        // under X1 and made every post-relogin heartbeat answer roomInstance:null,
        // flipping the client's presence check into "local presence is out-of-sync".
        // Every presence request carries Bearer sub=1 (reference SIM captures), so
        // accountId is the reliable assignment key and LoginLock is only a fallback.
        if (accountId.HasValue)
            return "account:" + accountId.Value;

        if (loginLock != null)
            return loginLock;

        return null;
    }

    // Assign the default dorm room (boot flow: /matchmake/dorm, /quickplay).
    public static void AssignRoom(HttpContext context) =>
        AssignRoom(context, DormRoom(AccountHelper.GetAccountIdFromToken(context) ?? 0));

    // The room this session is currently assigned to, for endpoints that must tag
    // a new resource with it (e.g. a share-camera photo is filed under the room the
    // player is standing in). Falls back to the static dorm id before assignment.
    public static int AssignedRoomIdFor(HttpContext context)
    {
        var key = SessionKey(context);
        if (key != null && _roomAssigned.TryGetValue(key, out var r) && r.RoomId != 0)
            return r.RoomId;
        return RoomId;
    }

    // Assign EXACTLY the room handed out: the instance JSON built by the caller
    // is stored verbatim so heartbeat echoes back precisely what the client got.
    public static void AssignRoom(HttpContext context, string roomInstanceJson)
        => AssignRoom(context, ParseRoomJson(roomInstanceJson) ?? DormRoom(AccountHelper.GetAccountIdFromToken(context) ?? 0));

    private static void AssignRoom(HttpContext context, AssignedRoom room)
    {
        var key = SessionKey(context);
        if (key != null)
        {
            // A fresh assignment always means the client has not joined yet: presence
            // must stay null until that reportjoinresult result=0 confirms the join.
            room.Joined = false;
            _roomAssigned[key] = room;
        }
    }

    // Mark the current assignment as actually joined (reportjoinresult result=0).
    public static void MarkRoomJoined(HttpContext context)
    {
        var key = SessionKey(context);
        if (key != null && _roomAssigned.TryGetValue(key, out var r) && r != null)
            r.Joined = true;
    }

    // True only when the assigned room has been confirmed joined by the client.
    public static bool HasRoomJoined(HttpContext context)
    {
        var key = SessionKey(context);
        return key != null && _roomAssigned.TryGetValue(key, out var r) && r != null && r.Joined;
    }

    public static void ClearRoom(HttpContext context)
    {
        var key = SessionKey(context);
        if (key != null) _roomAssigned.TryRemove(key, out _);
    }

    public static bool HasRoomAssigned(HttpContext context)
    {
        var key = SessionKey(context);
        return key != null && _roomAssigned.ContainsKey(key);
    }

    // True only when the caller holds a REAL assignment whose room is a Dorm.
    // Deliberately not AssignedRoomIdFor: that falls back to the static dorm id
    // when nothing is assigned, which would report "standing in my own dorm" for
    // a player who was never handed one - and would hand out creation rights in
    // public rooms. Dorm identity is per-account (see DormInstanceId), so any
    // assigned Dorm is the caller's own private room.
    public static bool IsInDorm(HttpContext context)
    {
        var key = SessionKey(context);
        if (key == null) return false;
        if (!_roomAssigned.TryGetValue(key, out var r) || r == null) return false;
        return RoomCatalog.IsDorm(r.RoomId);
    }

    // The room the caller currently holds (assigned by /matchmake/*), verbatim.
    // With NO assignment returns "" — presence readers guard on HasRoomAssigned and
    // answer roomInstance:null, so the state is never invented (the old fallback to
    // the static dorm made /matchmake/none and heartbeats advertise a dorm the
    // client was never told about).
    public static string RoomJson(HttpContext context)
    {
        var key = SessionKey(context);
        if (key != null && _roomAssigned.TryGetValue(key, out var r) && r != null && !string.IsNullOrEmpty(r.RoomJson))
            return r.RoomJson;
        return "";
    }

    // Static dorm JSON (default advertisement, e.g. for other players in
    // /player presence lookups).
    public static string RoomJson() => DefaultRoomJson();

    public static string PhotonRoomIdFor(HttpContext context, string queryFallback = "")
    {
        var key = SessionKey(context);
        if (key != null && _roomAssigned.TryGetValue(key, out var r) && r != null && !string.IsNullOrEmpty(r.PhotonRoomId))
            return r.PhotonRoomId;
        return string.IsNullOrEmpty(queryFallback) ? "" : queryFallback;
    }

    public static string RegionFor(HttpContext context)
    {
        var key = SessionKey(context);
        if (key != null && _roomAssigned.TryGetValue(key, out var r) && r != null && !string.IsNullOrEmpty(r.Region))
            return r.Region;
        return PhotonCloudConfig.Region;
    }

    public static bool TryGetAssignedInstanceId(HttpContext context, out long instanceId)
    {
        instanceId = 0;
        var key = SessionKey(context);
        if (key == null || !_roomAssigned.TryGetValue(key, out var r) || r == null) return false;
        instanceId = r.RoomInstanceId;
        return true;
    }

    private static AssignedRoom DormRoom() => DormRoom(0);

    private static AssignedRoom DormRoom(int accountId)
    {
        return new AssignedRoom
        {
            // Per-account: never share the dorm instance id or the Photon world
            // with another player.
            RoomInstanceId = DormInstanceId(accountId),
            PhotonRoomId = DormPhotonRoomId(accountId),
            RoomName = AccountHelper.DormName(accountId),
            RoomJson = DefaultRoomJson(accountId)
        };
    }

    private static string DefaultRoomJson() => DefaultRoomJson(0);

    private static string DefaultRoomJson(int accountId)
    {
        var region = PhotonCloudConfig.Region;
        // The dorm is titled after its owner ("@CosmicPixel3625's Dorm" in the
        // reference); the literal "@Player's Dorm" placeholder read as a bug.
        var roomName = (AccountHelper.DormName(accountId) ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        // Per-account instance id + Photon room, so this player owns this dorm.
        var instanceId = DormInstanceId(accountId);
        var photonRoomId = DormPhotonRoomId(accountId);
        return $@"{{
  ""roomInstanceId"":{instanceId},""roomId"":{RoomId},""subRoomId"":{SubRoomId},""roomInstanceType"":{RoomInstanceType},
  ""location"":""{RoomLocation}"",""dataBlob"":"""",""eventId"":0,""clubId"":0,""roomCode"":"""",
  ""photonRegion"":""{region}"",""photonRegionId"":""{region}"",""photonRoomId"":""{photonRoomId}"",
  ""name"":""{roomName}"",""maxCapacity"":4,""isFull"":false,""isPrivate"":true,""isInProgress"":false,""EncryptVoiceChat"":false
}}";
    }

    private static AssignedRoom ParseRoomJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            return new AssignedRoom
            {
                RoomInstanceId = ReadLong(root, "roomInstanceId", RoomData.RoomInstanceId),
                RoomId = ReadInt(root, "roomId", RoomData.RoomId),
                SubRoomId = ReadInt(root, "subRoomId", RoomData.SubRoomId),
                RoomInstanceType = ReadInt(root, "roomInstanceType", RoomData.RoomInstanceType),
                RoomLocation = ReadString(root, "location", RoomData.RoomLocation),
                RoomName = ReadString(root, "name", "@Player's Dorm"),   // placeholder; real name comes from the JSON when present
                IsPrivate = ReadBool(root, "isPrivate", true),
                MaxCapacity = ReadInt(root, "maxCapacity", 4),
                Region = ReadString(root, "photonRegion", PhotonCloudConfig.Region),
                PhotonRoomId = ReadString(root, "photonRoomId", ""),
                RoomJson = json
            };
        }
        catch
        {
            return null;
        }
    }

    private static long ReadLong(JsonElement root, string name, long fallback)
    {
        if (root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out var v)) return v;
        return fallback;
    }

    private static int ReadInt(JsonElement root, string name, int fallback)
    {
        if (root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var v)) return v;
        return fallback;
    }

    private static string ReadString(JsonElement root, string name, string fallback)
    {
        if (root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String) return el.GetString() ?? fallback;
        return fallback;
    }

    private static bool ReadBool(JsonElement root, string name, bool fallback)
    {
        if (root.TryGetProperty(name, out var el))
        {
            if (el.ValueKind == JsonValueKind.True) return true;
            if (el.ValueKind == JsonValueKind.False) return false;
            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var v)) return v != 0;
        }
        return fallback;
    }
}