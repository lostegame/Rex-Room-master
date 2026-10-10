namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;

// GET /rooms/{roomId} — full room object (captured schema). Prefix catch-all
// for dynamic room ids. The more specific /rooms/hot, /rooms/search etc. handlers
// win via longest-match routing in Server.cs.
public class RoomServerRoomsApi : IApi
{
    private readonly string _url;
    public RoomServerRoomsApi(string url = "/rooms/{roomId}") { _url = url; }

    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 2056;

        int roomId = RoomData.RoomId; // default dorm
        string path = context.Request.Path.Value ?? "";
        var segs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // The room id is the single segment immediately after "rooms" so it works
        // for both /rooms/{id} and /roomserver/rooms/{id}.
        for (int i = segs.Length - 1; i >= 0; i--)
        {
            if (string.Equals(segs[i], "rooms", StringComparison.OrdinalIgnoreCase) && i + 1 < segs.Length)
            {
                if (int.TryParse(segs[i + 1], out int parsed)) roomId = parsed;
                break;
            }
        }

        string created = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z";
        bool isDorm = roomId == RoomData.RoomId || roomId == 3041;

        // Resolve from the seeded room catalog first (Rec Center, Paintball, ...).
        if (RoomCatalog.TryGetRoom(roomId, out var catalogRoom))
            return RoomCatalog.RoomJson(catalogRoom, accountId);

        return $@"{{
  ""RoomId"":{roomId},""Name"":""{(isDorm ? AccountHelper.DormName(accountId).Replace("\\", "\\\\").Replace("\"", "\\\"") : "Room " + roomId)}"",""Description"":""{(isDorm ? "Your private room" : "")}"",""ImageName"":""{(isDorm ? "DormRoom.jpg" : "")}"",
  ""WarningMask"":0,""CustomWarning"":null,""CreatorAccountId"":{accountId},""State"":0,""Accessibility"":{(isDorm ? 2 : 1)},""PublishState"":0,
  ""SupportsLevelVoting"":false,""IsRRO"":true,""IsRecRoomApproved"":false,""ExcludeFromLists"":false,""ExcludeFromSearch"":false,
  ""SupportsScreens"":true,""SupportsWalkVR"":true,""SupportsTeleportVR"":true,""SupportsVRLow"":true,""SupportsQuest2"":true,""SupportsMobile"":true,""SupportsJuniors"":true,
  ""MinLevel"":0,""AgeRating"":2,""CreatedAt"":""{created}"",""PublishedAt"":""{created}"",""BecameRRStudioRoomAt"":null,""RankingContext"":null,
  ""IsDorm"":{(isDorm ? "true" : "false")},""IsPlacePlay"":false,""MaxPlayerCalculationMode"":0,""MaxPlayers"":4,""CloningAllowed"":false,""DisableMicAutoMute"":false,""DisableRoomComments"":false,""EncryptVoiceChat"":false,
  ""ToxmodEnabled"":true,""LoadScreenLocked"":false,""UgcVersion"":1,""PersistenceVersion"":0,""UgcSubVersion"":null,""MinUgcSubVersion"":null,
  ""AutoLocalizeRoom"":false,""LocalizationContext"":{{""TargetLocale"":null,""Scope"":null,""LocalizedFields"":[""Description"",""CustomWarning""]}},
  ""IsDeveloperOwned"":true,""RankedEntityId"":""{(isDorm ? "1" : roomId.ToString())}"",""Roles"":[{{""AccountId"":{accountId},""Role"":255,""LastChangedByAccountId"":null,""InvitedRole"":0}}],
  ""IsJuniorCreated"":false,""PromoImages"":[],""PromoExternalContent"":[],""LoadScreens"":[],""RestrictedCircuitsAllowListNames"":[],""BoostCount"":0,""CurrentSnapshotId"":null,
  ""CCU"":null,""Stats"":{{""CheerCount"":0,""FavoriteCount"":0,""VisitorCount"":0,""VisitCount"":1}},
  ""SubRooms"":[{{""CreatorAccountId"":{accountId},""UnitySceneId"":""{RoomData.RoomLocation}"",""Name"":""Home"",""LastModeratedSaveModerationState"":0,""IsSandbox"":true,""MaxPlayers"":4,""Accessibility"":{(isDorm ? 2 : 1)},""ShouldAutoStageSaves"":true,""RoomId"":{roomId},""SubRoomId"":{RoomData.SubRoomId},""StagedSubRoomDataSaveId"":null,""CurrentSave"":null}}],
  ""Tags"":[]
}}";
    }

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
}
