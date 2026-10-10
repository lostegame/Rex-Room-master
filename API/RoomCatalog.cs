namespace Rex_Room_20230414.API;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

// Room catalog seeded from Content/ImportRooms.json (the reference server's room
// seed, verbatim). Loaded once at first use. The 2023 client needs the room DTO
// shape RoomServerRoomsApi already emits; this class keeps that shape but fills
// the real data for the rooms the reference ships (Rec Center, Paintball, ...).
public static class RoomCatalog
{
    private static List<JsonElement>? _rooms;
    private static readonly object _lock = new();

    private static List<JsonElement> Rooms
    {
        get
        {
            if (_rooms != null) return _rooms;
            lock (_lock)
            {
                if (_rooms != null) return _rooms;
                _rooms = Load();
            }
            return _rooms;
        }
    }

    private static List<JsonElement> Load()
    {
        var list = new List<JsonElement>();
        try
        {
            string path = ResolveContentPath("ImportRooms.json");
            if (File.Exists(path))
            {
                using var doc = JsonDocument.Parse(File.ReadAllBytes(path));
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    list = doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RoomCatalog] failed to load: {ex.Message}");
        }
        return list;
    }

    private static string ResolveContentPath(string file)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Content", file),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", file))
        };
        foreach (var c in candidates)
            if (File.Exists(c)) return c;
        return candidates[0];
    }

    public static bool TryGetRoom(int roomId, out JsonElement room)
    {
        var found = Rooms.FirstOrDefault(r =>
            r.TryGetProperty("RoomId", out var id) && id.TryGetInt32(out var i) && i == roomId);
        room = Clone(found);
        return room.ValueKind == JsonValueKind.Object;
    }

    // The room's real first SubRoomId.
    //
    // Needed because a matchmade instance has to point at a subroom that actually
    // exists in the room DTO, and only 1 of the 46 seeded rooms has SubRoomId 1.
    // Hardcoding 1 made 45 rooms fail to load with reportjoinresult=3.
    public static int FirstSubRoomId(JsonElement room)
    {
        if (room.ValueKind != JsonValueKind.Object) return 0;
        if (room.TryGetProperty("SubRooms", out var subs) && subs.ValueKind == JsonValueKind.Array)
        {
            foreach (var sub in subs.EnumerateArray())
            {
                if (sub.ValueKind == JsonValueKind.Object &&
                    sub.TryGetProperty("SubRoomId", out var s) && s.TryGetInt32(out int id))
                    return id;
            }
        }
        if (room.TryGetProperty("SubRoomId", out var own) && own.TryGetInt32(out int direct))
            return direct;
        return 0;
    }

    public static int FirstSubRoomId(int roomId) =>
        TryGetRoom(roomId, out var r) ? FirstSubRoomId(r) : 0;

    public static bool TryGetRoomByName(string name, out JsonElement room)
    {
        var found = Rooms.FirstOrDefault(r =>
            r.TryGetProperty("Name", out var n) &&
            string.Equals(n.GetString(), name, StringComparison.OrdinalIgnoreCase));
        room = Clone(found);
        return room.ValueKind == JsonValueKind.Object;
    }

    public static JsonElement[] Hot(string tag = "", int skip = 0, int take = 100)
    {
        // EnumerateToArray() copies the elements, but LINQ over the copy still
        // points at the disposed JsonDocument, so Clone() each element: a
        // JsonElement is only valid while its document lives.
        var q = Rooms.Where(r => !HasTrue(r, "IsDorm"));
        if (!string.IsNullOrEmpty(tag))
            q = q.Where(r => r.TryGetProperty("Tags", out var tags) &&
                             tags.EnumerateArray().Any(t =>
                                t.TryGetProperty("Tag", out var t2) &&
                                string.Equals(t2.GetString(), tag, StringComparison.OrdinalIgnoreCase)));
        return q.Skip(skip).Take(Math.Max(1, take)).Select(Clone).ToArray();
    }

    public static JsonElement[] Search(string name, int skip = 0, int take = 100)
    {
        var q = Rooms.AsEnumerable();
        if (!string.IsNullOrEmpty(name))
            q = q.Where(r => r.TryGetProperty("Name", out var n) &&
                             n.GetString()?.Contains(name, StringComparison.OrdinalIgnoreCase) == true);
        return q.Skip(skip).Take(Math.Max(1, take)).Select(Clone).ToArray();
    }

    public static JsonElement[] OwnedBy(int accountId)
    {
        return Rooms.Where(r => r.TryGetProperty("CreatorAccountId", out var c) &&
                                c.TryGetInt32(out var ci) && ci == accountId).Select(Clone).ToArray();
    }

    // Rooms related to the given one: same primary genre tag where possible,
    // otherwise anything public. Excludes the room itself and dorms.
    public static JsonElement[] Similar(int roomId, int take = 6)
    {
        var self = Rooms.FirstOrDefault(r => r.TryGetProperty("RoomId", out var ri) &&
                                             ri.TryGetInt32(out var id) && id == roomId);
        string genre = null;
        if (self.ValueKind == JsonValueKind.Object &&
            self.TryGetProperty("Tags", out var selfTags) &&
            selfTags.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in selfTags.EnumerateArray())
            {
                if (t.TryGetProperty("Tag", out var tag) && tag.GetString() is string s &&
                    !s.Equals("rro", StringComparison.OrdinalIgnoreCase))
                { genre = s; break; }
            }
        }

        var q = Rooms.Where(r => !HasTrue(r, "IsDorm") &&
                                 !(r.TryGetProperty("RoomId", out var ri2) &&
                                   ri2.TryGetInt32(out var id2) && id2 == roomId));
        if (genre != null)
            q = q.Where(r => r.TryGetProperty("Tags", out var tags) &&
                             tags.ValueKind == JsonValueKind.Array &&
                             tags.EnumerateArray().Any(t =>
                                t.TryGetProperty("Tag", out var t2) &&
                                string.Equals(t2.GetString(), genre, StringComparison.OrdinalIgnoreCase)));
        return q.Take(Math.Max(1, take)).Select(Clone).ToArray();
    }

    private static bool HasTrue(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

    // A JsonElement is a view into its JsonDocument; Clone() detaches it so it
    // stays valid after the loader's `using` block disposes the document.
    private static JsonElement Clone(JsonElement e) =>
        e.ValueKind == JsonValueKind.Object || e.ValueKind == JsonValueKind.Array ? e.Clone() : e;

    public static bool HasRoom(int roomId) => TryGetRoom(roomId, out _);

    // The single canonical dorm test, shared with the dorm name helpers and the
    // per-entry Dorm reward so all three agree on what "the Dorm" is.
    public static bool IsDorm(int roomId)
    {
        if (roomId == RoomData.RoomId) return true;
        return TryGetRoom(roomId, out var room) && Bool(room, "IsDorm");
    }

    public static int CreatorOf(int roomId)
    {
        if (TryGetRoom(roomId, out var room))
            return Int(room, "CreatorAccountId", 1);
        return 1;
    }

    // Builds the wire room DTO in exactly the shape RoomServerRoomsApi returns for
    // the 2023 client, but with the catalog entry's real values.
    public static string RoomJson(JsonElement room, int viewerAccountId)
    {
        int roomId = Int(room, "RoomId", 0);
        string name = Str(room, "Name", "Room " + roomId);
        string description = Str(room, "Description", "");
        string image = Str(room, "ImageName", "");
        int creator = Int(room, "CreatorAccountId", 1);
        bool isDorm = Bool(room, "IsDorm") || roomId == RoomData.RoomId;
        // A dorm is named after its owner. The catalog entry ships with the
        // placeholder "@Player's Dorm", which is what used to render on the
        // dorm banner instead of the player's own name.
        if (isDorm && viewerAccountId != 0) name = AccountHelper.DormName(viewerAccountId);
        int accessibility = Int(room, "Accessibility", isDorm ? 2 : 1);
        int maxPlayers = Int(room, "MaxPlayers", isDorm ? 4 : 12);
        string rankedId = Str(room, "RankedEntityId", roomId.ToString());
        string created = Str(room, "CreatedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
        string published = Str(room, "PublishedAt", created);

        var stats = room.TryGetProperty("Stats", out var s) ? s : default;
        int cheer = stats.ValueKind == JsonValueKind.Object && stats.TryGetProperty("CheerCount", out var ch) ? ch.GetInt32() : 0;
        int fav = stats.ValueKind == JsonValueKind.Object && stats.TryGetProperty("FavoriteCount", out var f) ? f.GetInt32() : 0;
        int visits = stats.ValueKind == JsonValueKind.Object && stats.TryGetProperty("VisitCount", out var v) ? v.GetInt32() : 1;

        string isDormStr = isDorm ? "true" : "false";
        string subRooms = SubRoomsJson(room, roomId, isDorm, maxPlayers, accessibility);

        return $@"{{
  ""RoomId"":{roomId},""Name"":""{JsonEsc(name)}"",""Description"":""{JsonEsc(description)}"",""ImageName"":""{JsonEsc(image)}"",
  ""WarningMask"":0,""CustomWarning"":null,""CreatorAccountId"":{creator},""State"":0,""Accessibility"":{accessibility},""PublishState"":0,
  ""SupportsLevelVoting"":false,""IsRRO"":true,""IsRecRoomApproved"":false,""ExcludeFromLists"":false,""ExcludeFromSearch"":false,
  ""SupportsScreens"":true,""SupportsWalkVR"":true,""SupportsTeleportVR"":true,""SupportsVRLow"":true,""SupportsQuest2"":true,""SupportsMobile"":true,""SupportsJuniors"":true,
  ""MinLevel"":0,""AgeRating"":2,""CreatedAt"":""{created}"",""PublishedAt"":""{published}"",""BecameRRStudioRoomAt"":null,""RankingContext"":null,
  ""IsDorm"":{isDormStr},""IsPlacePlay"":false,""MaxPlayerCalculationMode"":0,""MaxPlayers"":{maxPlayers},""CloningAllowed"":false,""DisableMicAutoMute"":false,""DisableRoomComments"":false,""EncryptVoiceChat"":false,
  ""ToxmodEnabled"":true,""LoadScreenLocked"":false,""UgcVersion"":1,""PersistenceVersion"":0,""UgcSubVersion"":null,""MinUgcSubVersion"":null,
  ""AutoLocalizeRoom"":false,""LocalizationContext"":{{""TargetLocale"":null,""Scope"":null,""LocalizedFields"":[""Description"",""CustomWarning""]}},
  ""IsDeveloperOwned"":true,""RankedEntityId"":""{rankedId}"",
  ""Roles"":[{{""AccountId"":{creator},""Role"":255,""LastChangedByAccountId"":null,""InvitedRole"":0}},{{""AccountId"":2,""Role"":30,""LastChangedByAccountId"":null,""InvitedRole"":0}}],
  ""IsJuniorCreated"":false,""PromoImages"":[],""PromoExternalContent"":[],""LoadScreens"":[],""RestrictedCircuitsAllowListNames"":[],""BoostCount"":0,""CurrentSnapshotId"":null,
  ""FriendlyName"":""{JsonEsc(name)}"",""CCU"":null,""Stats"":{{""CheerCount"":{cheer},""FavoriteCount"":{fav},""VisitorCount"":0,""VisitCount"":{visits}}},
  ""SubRooms"":{subRooms},
  ""Tags"":[{{""Tag"":""rro"",""Type"":2}}]
}}";
    }

    // Serializes the catalog's own SubRooms instead of collapsing them into a
    // single placeholder. The reference (rex room 3041 body) keeps one entry per
    // subroom with its REAL SubRoomId and UnitySceneId (RecCenter is 2, not 1),
    // and the client fetches /rooms/{id}/subrooms/{subRoomId}/data using that id,
    // so a hardcoded 1 made the room-data lookup miss and the load hang.
    private static string SubRoomsJson(JsonElement room, int roomId, bool isDorm,
                                       int maxPlayers, int accessibility)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append('[');

        int n = 0;
        if (room.TryGetProperty("SubRooms", out var subs) && subs.ValueKind == JsonValueKind.Array)
        {
            foreach (var sub in subs.EnumerateArray())
            {
                int subRoomId = Int(sub, "SubRoomId", n + 1);
                string subScene = Str(sub, "UnitySceneId", "");
                string subName = Str(sub, "Name", "Home");
                bool subSandbox = Bool(sub, "IsSandbox") || (isDorm && n == 0);
                int subMax = Int(sub, "MaxPlayers", isDorm ? 4 : maxPlayers);
                int subAcc = Int(sub, "Accessibility", isDorm ? 2 : accessibility);
                bool autoStage = !sub.TryGetProperty("ShouldAutoStageSaves", out var asv) || !asv.Equals(false);
                int moderated = Int(sub, "LastModeratedSaveModerationState", 0);

                if (n > 0) sb.Append(',');
                n++;
                sb.Append('{')
                  .Append(@"""SubRoomId"":").Append(subRoomId)
                  .Append(@",""RoomId"":").Append(roomId)
                  .Append(@",""CreatorAccountId"":null")
                  .Append(@",""UnitySceneId"":""").Append(JsonEsc(subScene)).Append('"')
                  .Append(@",""Name"":""").Append(JsonEsc(subName)).Append('"')
                  .Append(@",""LastModeratedSaveModerationState"":").Append(moderated)
                  .Append(@",""IsSandbox"":").Append(subSandbox ? "true" : "false")
                  .Append(@",""MaxPlayers"":").Append(subMax)
                  .Append(@",""Accessibility"":").Append(subAcc)
                  .Append(@",""ShouldAutoStageSaves"":").Append(autoStage ? "true" : "false")
                  .Append(@",""StagedSubRoomDataSaveId"":null")
                  .Append(@",""CurrentSave"":").Append(
                      // Official rooms have no save; the player's own room does.
                      isDorm ? DormCurrentSave(roomId, subRoomId) : "null")
                  .Append('}');
            }
        }

        if (n == 0)
        {
            sb.Append('{')
              .Append(@"""SubRoomId"":1,""RoomId"":").Append(roomId)
              .Append(@",""CreatorAccountId"":null")
              .Append(@",""UnitySceneId"":""").Append(JsonEsc(RoomData.RoomLocation)).Append('"')
              .Append(@",""Name"":""Home"",""LastModeratedSaveModerationState"":0")
              .Append(@",""IsSandbox"":").Append(isDorm ? "true" : "false")
              .Append(@",""MaxPlayers"":").Append(isDorm ? 4 : maxPlayers)
              .Append(@",""Accessibility"":").Append(isDorm ? 2 : accessibility)
              .Append(@",""ShouldAutoStageSaves"":true,""StagedSubRoomDataSaveId"":null")
              .Append(@",""CurrentSave"":").Append(isDorm ? DormCurrentSave(roomId, 1) : "null")
              .Append('}');
        }

        sb.Append(']');
        return sb.ToString();
    }

    // The player's own room always carries a save. The client asks for
    // /rooms/{id}/subrooms/{subRoomId}/data and then downloads RoomDataBlob, so a
    // null here left the room waiting on the instance-data download.
    private static string DormCurrentSave(int roomId, int subRoomId)
    {
        return $@"{{""UnitySubAssets"":[],""ReferencedUnityAssets"":[],""DataBlob"":""{RoomData.RoomDataBlob}"",""DataBlobHash"":null,""ReferencedUnityAssetIds"":[],""PersistenceVersion"":51,""OMVersion"":0,""UgcSubVersion"":0,""SavedByAccountId"":0,""SavedOnPlatform"":0,""SavedOnDeviceClass"":0,""Description"":"""",""Tags"":[],""ModerationState"":0,""CreatedAt"":""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fff}Z"",""SubRoomDataSaveId"":{RoomData.SubRoomDataSaveId},""SubRoomId"":{subRoomId}}}";
    }

    public static string ListJson(JsonElement[] rooms, int viewer)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append('[');
        for (int i = 0; i < rooms.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(RoomJson(rooms[i], viewer));
        }
        sb.Append(']');
        return sb.ToString();
    }

    public static string PagedJson(JsonElement[] rooms, int viewer)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("{\"Results\":[");
        for (int i = 0; i < rooms.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(RoomJson(rooms[i], viewer));
        }
        sb.Append("],\"TotalResults\":").Append(rooms.Length).Append('}');
        return sb.ToString();
    }

    private static bool Bool(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

    private static int Int(JsonElement e, string name, int fallback) =>
        e.TryGetProperty(name, out var v) && v.TryGetInt32(out var i) ? i : fallback;

    private static string Str(JsonElement e, string name, string fallback) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? fallback : fallback;

    private static string? FirstSubRoomScene(JsonElement e)
    {
        if (e.TryGetProperty("SubRooms", out var subs) && subs.ValueKind == JsonValueKind.Array)
        {
            foreach (var sub in subs.EnumerateArray())
                if (sub.TryGetProperty("UnitySceneId", out var s) && s.ValueKind == JsonValueKind.String)
                    return s.GetString();
        }
        return null;
    }

    private static string JsonEsc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}