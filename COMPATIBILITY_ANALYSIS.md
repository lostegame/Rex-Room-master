# Rex-Room-20230414 ⇄ RecFlare Client Compatibility Analysis

## 1. Current State of Rex-Room-20230414 (as-found)

Rex-Room-20230414 is a **.NET 10 Kestrel** server (`net10.0`). It currently **does not compile** (82 errors).

**Root causes:**
1. `Server.cs` registers **86 API handler classes** in `ApiList`, but only **15** exist as files in `API\`. **71 classes are missing entirely.**
2. `IApi` interface declares only `Response`, `GetUrl`, `GetMethod` — but `Server.cs:199` calls `api.GetStatusCode()`, which does not exist.
3. `DatabaseManager` only implements `SaveAsync(identifier,key,data)` / `LoadAsync(identifier,key)` — but `AccountHelper`, `AccountCreateApi`, `ConnectTokenApi`, and `Server` call methods that do not exist: `LoadAllPlayerData()`, `SavePlayerData(Account)`, `LoadUsernames()`, `RegisterUsername(string)`, `GetProfileImage(int)`.

**Grouping of the 15 existing handler files:**
- Core/env: `DefaultApi`(/), `TestApi`(/test), `AmplitudeApi`, `ConfigV2Api`, `VersionCheckApi`, `CharadesWordsApi`, `ClubHomeMeApi`
- Auth/account: `ConnectTokenApi`, `CachedLoginApi`, `AccountMeApi`, `AccountCreateApi`, `AccountBulkApi`, `AccountHelper`, `Account`
- Avatar: `AvatarApi`, `AvatarDefaultApi`, `AvatarItemsApi`

**Intent of the project (from README):** universal/modular backend for RecRoom, targeting 2016 first then up toward (implied) newer builds. Despite the README targeting 2016, several handlers already point at modern endpoints (`/api/avatar/v4/items`, `/api/config/v2`, `/api/versioncheck`), so the codebase is mid-disruption between old and new generations.

---

## 2. The Target Client (from our RecFlare capture)

- **Game build `20230414`**, Unity 2020.3.33f1, Running via a BepInEx plugin that redirects the official `*.rec.net` hosts to `*.recflare.net`.
- The client issues requests to these sub-hosts: `auth`, `api`, `accounts`, `econ`, `match`, `rooms`, `clubs`, `chat`, `playersettings`, `platformnotifications`, `notify` (SignalR), `img` (image CDN), `datacollection` (dropped).
- Auth flow observed: `create_account` on first run, then `cached_login` on relaunch; JWT HS256; `LoginLock` form param on all `match.*` calls.

---

## 3. Infrastructure Fixes Required (block all compilation)

### 3.1 `IApi` — add `GetStatusCode()`
`Server.cs` needs `api.GetStatusCode()`. Add an interface member with a default implementation (C# 8+ default interface methods, or make every handler supply it). Cleanest: give interface a **default** implementation returning 200 so only handlers that override it provide a non-200:

```csharp
public interface IApi
{
    public string Response(HttpContext context);
    public string GetUrl();
    public string GetMethod();
    public int GetStatusCode() => 200;
}
```

### 3.2 `DatabaseManager` — add missing methods
Needed by `AccountHelper`, `AccountCreateApi`, `ConnectTokenApi`, `Server`:
- `LoadAllPlayerData()` → `List<Account>` (scan `Storage\*\{Account}.json` or a master store)
- `SavePlayerData(Account acc)`
- `LoadUsernames()` → `List<string>`
- `RegisterUsername(string username)`
- `GetProfileImage(int accountId)` → `byte[]?`

Also must map the `SaveAsync/LoadAsync(identifier,key)` model onto the Account model (store account JSON under `Storage\{accountId}\account.json`).

---

## 4. Endpoint Response-Shape Corrections (existing handlers vs. captured RecFlare)

### 4.1 `VersionCheckApi`
- **Rex-Room-20230414:** `GET /api/versioncheck/v1`, returns `{"ValidVersion":bool}`, reads header `X-Rec-Room-Version`, expects `20170626_EA`.
- **Captured:** `GET /api/versioncheck/v4?v=20230414&p=0&pid=2056` →
  `{"VersionStatus":0,"UpdateNotificationStage":0,"IsVersionIslanded":false,"IsCrossPlayDisabled":false}`
- **Fix:** change URL to `/api/versioncheck/v4` and return the captured schema.

### 4.2 `AmplitudeApi`
- **Captured:** `{"AmplitudeKey":"","UseRudderStack":false,"RudderStackKey":"","UseStatSig":false,"StatSigKey":"","StatSigEnvironment":0}` (all blank).
- **Rex-Room-20230414:** fills in non-empty keys. For strict client parity use empty keys (RecFlare returns blanks and client tolerates it).

### 4.3 `AvatarDefaultApi` (/api/avatar/v1/defaultunlocked)
- Captured element schema: `{"AvatarItemType":0,"AvatarItemDesc":"<guid>,,,","PlatformMask":-1,"FriendlyName":"...","Tooltip":"","Rarity":0}` (+ `PlatformMask`). Rex-Room-20230414 has `Favorited`/`UnlockedLevel` and omits `PlatformMask`. Should add `PlatformMask` and use real captured GUIDs (from `/api/avatar/v1/defaultunlocked` full body in log).

### 4.4 `AvatarApi` (/api/avatar/v2)
- Captured includes `OutfitSelections`, `OutfitSelectionsV2`, `FaceFeatures`, `SkinColor`, `HairColor`, `CustomAvatarItems`. Rex-Room-20230414 omits `OutfitSelectionsV2` and `CustomAvatarItems`.

### 4.5 `AccountMeApi` (/account/me) and `AccountBulkApi` (/account/bulk)
- Captured `/account/me`: `accountId, username, displayName, profileImage, bannerImage, displayEmoji, isJunior, platforms(0=number!), personalPronouns, identityFlags, createdAt, email, birthday, availableUsernameChanges`.
- Rex-Room-20230414 uses `platforms:[{...}]` array, adds `registrationStatus`/`isNew`, misses `email`/`birthday`/`availableUsernameChanges`/`bannerImage`.

### 4.6 `CachedLoginApi` (/cachedlogin/forplatformid/...)
- Captured returns an **array of ALL accounts** on the platformId (we saw TWO: 2055 & 2056). Rex-Room-20230414 uses `FirstOrDefault` → returns at most one. Must return all accounts for the platformId.

### 4.7 `ClubHomeMeApi` (/club/home/me)
- In our capture `GET /club/home/me` returned **404** (stub). Keeping it is fine, but Rex-Room-20230414 currently returns 200 with fake data.

---

## 5. Missing Handler Classes — Mapped to the Client

The following 71 classes are **referenced but missing**. Those marked **[CAPTURED]** correspond to endpoints we actually observed the 2023.4 client call; those marked **[INTERNAL]** are never seen in our capture (used by OTHER/Old Rex Room 20230414 features), and per the instruction "do not assume endpoints not in our data", such handlers may return a minimal `{}`/`[]` (client never calls them) — but they must exist so the project compiles.

### 5.1 Login / Launch critical (captured, must be correct)
- `EacChallengeApi` → `GET /eac/challenge` → `"AA=="` (captured)
- `GameConfigsApi` → config endpoints (captured: `/api/rooms/v1/serverconfigurations`, `getSessionConfigs`, etc.)
- `HeartbeatApi` → `POST /player/heartbeat` (captured: room instance JSON)
- `PlayerLoginApi` → `POST /player/login` → `{}`/empty (captured)
- `PlayerLogoutApi` → `POST /player/logout` (captured)
- `HubNegotiateApi` → `POST /hub/v1/negotiate` → SignalR negotiate JSON (captured)
- `ModerationBlockDetailsApi` → `GET /api/PlayerReporting/v1/moderationBlockDetails` (captured full JSON)
- `ObjectivesMyProgressApi` → `GET /api/objectives/v1/myprogress` (captured full JSON)
- `ChallengeCurrentApi` → `GET /api/checklist/v1/current` (captured)
- `ProgressionBulkApi` → `GET /api/players/v2/progression/bulk?id=` (captured: `[{PlayerId,Level,XP}]`)
- `PlayerReputationBulkApi` → `GET /api/playerReputation/v2/bulk?id=` (captured)
- `RelationshipsGetApi` → `GET /api/relationships/v2/get` (captured: `[]`)
- `RoomFiltersApi` → `GET /api/rooms/v1/filters` (captured)
- `CachedLoginForPlatformIdsApi` → `GET /cachedlogin/forplatformid/...` (plural wrapper; captured array)
- `AvatarGiftsApi` → `POST /api/avatar/v2/gifts/consume/` (captured)
- `AvatarSavedApi` → `POST /api/avatar/v4/saved/set` (captured)
- `AvatarSetApi` → avatar save/publish (captured variants)
- `GotoNoneApi` → `POST /matchmake/none` (captured)
- `GotoRoomApi` / `GotoRoomRecCenterApi` → `POST /matchmake/dorm` etc. (captured)
- `RoomInstanceReportJoinResultApi` → `POST /roominstance/{id}/reportjoinresult` (captured)
- `RoomServerRoomsApi` → `GET /rooms/{id}` (captured full room object)
- `WalletApi` → currency/balance (captured `/api/currency/v1/balances`)
- `StorefrontBalanceApi` → econ balance (captured)
- `GiftDropStoreApi` → `/api/giftdrops/v1/store` (captured)
- `EquipmentUnlockedApi` / `ConsumablesUnlockedApi` → `GET /api/avatar/v1/defaultunlocked` (captured)
- `SubscriptionMemberApi`, `AnnouncementsUnreadApi`, `AnnouncementsSubscriptionUnreadApi` → clubs membership/announcements (captured `[]`)
- `ThreadApi`, `ChatThreadApi`, `MessagesGetApi`, `MessagesFavoriteStatusApi` → chat (captured `GET /chat/thread`)
- `ProfileImageApi`, `ProfileImageUploadApi`, `ImagesUploadSavedApi`, `ImagesNamedApi`, `ImagesSlideshowApi` → images (captured image endpoints)
- `RoomKeysRoomApi`, `RoomKeysMineApi`, `RoomCurrenciesApi`, `RoomCurrenciesAllBalancesApi`, `RoomCurrenciesBetaApi` → dorm currencies/keys (captured empty arrays)
- `PageViewConsumeApi` → `POST link/pageview/consume` (captured **404** on RecFlare)
- `LoadingScreenTipDataApi` → tips (captured)
- `StatusVisibilityApi`, `AvoidJuniorsApi`, `BirthdayApi`, `ChangeUsernameApi`, `ChangePasswordApi`, `ForgetPasswordApi`, `ResetPasswordApi` → account settings (partially captured)
- `SettingsApi`, `SettingsSetApi` → playersettings (captured `GET/PUT/POST /playersettings`)
- `PlayerByIdApi` → `GET /player?id=` (captured)
- `PhotonRegionPingsApi` → pings (captured region pings)
- `QuickPlayApi` → match quickplay (captured)
- `CommunityBoardCurrentApi`, `AnnouncementGetApi`, `ClubCategoryTagsApi`, `ClubMineCreatedApi`, `ClubMineMemberApi`, `ClubSearchApi`, `ClubhouseSearchMostActiveNowApi` → clubs (captured arrays)
- `CampusCardApi`, `GameRewardsPendingApi`, `VoteToKickReasonsApi`, `FeaturedRoomsCurrentApi`, `PlayerEventsAllApi` → [INTERNAL/other-build, minimal stub ok]

### Build blockers (must exist, minimal) even if client never calls:
Everything in the list above must at least compile; for endpoints not in our capture return `[]`/`{}` (client never reached them).

---

## 6. Proposed Implementation Order

1. **Fix `IApi`** (add default `GetStatusCode`) — unblocks Server.cs.
2. **Fix `DatabaseManager`** — unblocks AccountHelper/ConnectToken/Server.
3. **Implement all 71 missing handler classes**, favoring captured schemas for login-critical ones (#5.1).
4. **Correct the 7 existing handlers** that mismatch captured shapes (#4).
5. **`dotnet build`** until clean.

Note: Real assets (room scenes, Photon auth, EAC beyond the echo, etc.) are out of scope; RecFlare itself stubs these (photon tokens null, `/eac/challenge` echo, analytics dropped). We match RecFlare's *stub contract*, not RecRoom's full prod contract.

---

## 7. Implementation Status (DONE — Rex-Room-20230414 now compiles, 0 errors)

### Infrastructure fixed
- **`API\IApi.cs`** — added `public int GetStatusCode() => 200;` (default interface method) so `Server.cs` compiles without each handler implementing it.
- **`DatabaseManager.cs`** — added `LoadAllPlayerData()`, `SavePlayerData(Account)`, `LoadUsernames()`, `RegisterUsername(string)`, `GetProfileImage(int)`, `SaveProfileImage(int,byte[])`. Accounts stored under `Storage\{accountId}\account.json`; usernames index at `Storage\__usernames.json`.
- **`Server.cs` routing** — overhauled from first-match `StartsWithSegments` to a **segment-scoring longest-match** algorithm (`MatchScore`). Each registered URL template is split into segments; `{wildcard}` segments match any one path segment. The handler with the highest score wins. This resolves the `/rooms/{id}` vs `/rooms/hot` vs `/rooms/search` collision that blocked the Watch/Play menu (see Section 8).

### All 97 handler classes (API\ = 101 .cs files, up from 18)

**Core (original) handlers:** DefaultApi, TestApi, CharadesWordsApi, AmplitudeApi, ConfigV2Api, VersionCheckApi, GameConfigsApi, ConnectTokenApi, AccountCreateApi, AccountMeApi, AccountBulkApi, AvatarApi, AvatarDefaultApi, AvatarItemsApi, SettingsApi, ClubHomeMeApi

**Launch/Auth (captured schemas):** EacChallengeApi (`"AA=="`), PlayerLoginApi, PlayerLogoutApi, HeartbeatApi (roomInstance JSON), HubNegotiateApi, ModerationBlockDetailsApi, ObjectivesMyProgressApi, ChallengeCurrentApi, ProgressionBulkApi, PlayerReputationBulkApi, RelationshipsGetApi, RoomFiltersApi, CachedLoginForPlatformIdsApi

**Avatar/Econ:** AvatarGiftsApi, AvatarSavedApi, AvatarSetApi, WalletApi, StorefrontBalanceApi, GiftDropStoreApi, EquipmentUnlockedApi, ConsumablesUnlockedApi

**Match/room (matchmake):** GotoRoomApi, GotoNoneApi, GotoRoomRecCenterApi, QuickPlayApi, PlayerByIdApi, RoomInstanceReportJoinResultApi, PhotonRegionPingsApi

**Room discovery (NEW — longest-match routing handles these correctly):** RoomHotApi (`/rooms/hot`), RoomSearchApi (`/rooms/search`), RoomCarouselRisingApi (`/rooms/carousel/rising`), RoomCreatedByMeApi (`/rooms/createdby/me`), RoomVisitedByMeApi (`/rooms/visitedby/me`), RoomFavoritedByMeApi (`/rooms/favoritedby/me`), RoomCuratedPlaylistsApi (`/rooms/curated_playlists`), RoomOwnedByMeApi (`/rooms/ownedby/{accountId}`), RoomPlayerDataMeApi (`/rooms/{roomId}/playerdata/me`), RoomPhotonAccessTokenApi (`/photon_access_token`), MatchRoomsRequiringApi (`/match/rooms/requiring/{type}`)

**Room data (dynamic):** RoomServerRoomsApi (`/rooms/{roomId}` — echoes requested id from path, returns full room object per captured schema)

**Settings/account:** SettingsApi, SettingsSetApi, ChangePasswordApi, ForgetPasswordApi, ResetPasswordApi, ChangeUsernameApi, StatusVisibilityApi, AvoidJuniorsApi, BirthdayApi, ProfileImageApi, ProfileImageUploadApi

**Chat/Messages:** ThreadApi, ChatThreadApi, MessagesGetApi, MessagesFavoriteStatusApi

**Clubs:** SubscriptionMemberApi, SubscriptionDetailsApi, AnnouncementsUnreadApi, AnnouncementsSubscriptionUnreadApi, AnnouncementGetApi, ClubCategoryTagsApi, ClubMineCreatedApi, ClubMineMemberApi, ClubSearchApi, ClubhouseSearchMostActiveNowApi, CommunityBoardCurrentApi

**Room currencies/keys:** RoomKeysRoomApi, RoomKeysMineApi, RoomCurrenciesApi, RoomCurrenciesAllBalancesApi, RoomCurrenciesBetaApi

**Images:** ImagesUploadSavedApi, ImagesNamedApi, ImagesSlideshowApi

**Misc:** LoadingScreenTipDataApi, PageViewConsumeApi, VoteToKickReasonsApi, FeaturedRoomsCurrentApi, PlayerEventsAllApi, CampusCardApi, GameRewardsPendingApi

**Shared helper:** `API\RoomData.cs` — dorm room instance (roomInstanceId, subRoomId, location, PhotonRoomId) used by Heartbeat, GotoRoom, PlayerById, RoomServerRooms.

### Existing handlers corrected to match captured schemas
- `VersionCheckApi` → `/api/versioncheck/v4` returning `{"VersionStatus":0,"UpdateNotificationStage":0,"IsVersionIslanded":false,"IsCrossPlayDisabled":false}`.
- `AmplitudeApi` → empty keys (RecFlare returns blanks).
- `AvatarDefaultApi` → real captured GUIDs + `PlatformMask` field.
- `AvatarApi` → added `OutfitSelectionsV2` + `CustomAvatarItems` (captured v2 schema).
- `AccountMeApi` / `AccountBulkApi` → camelCase, `platforms` as **number (0)**, plus `email`/`birthday`/`availableUsernameChanges`/`bannerImage`.
- `CachedLoginApi` → returns **all** accounts for a platformId (capture showed two accounts 2055 & 2056).
- `RoomServerRoomsApi` → **echoes the `{roomId}` from the path** (previously hardcoded to 3070).
- `FeaturedRoomsCurrentApi` → **404** with `{"success":false,"error":{"message":"not found"}}` (matches captured behavior).
- `RoomInstanceReportJoinResultApi` → explicit URL `/roominstance/{roomInstanceId}/reportjoinresult` (was bare `/roominstance/`).

### Known limitations remaining before a full client boot test
1. **Endpoints in the capture not yet registered** in `Server.cs` (e.g. `PUT /account/me/bio`, `PUT /account/me/displayname`, `PUT /account/me/personalpronouns`, econ `buyItem`, room `hot` search results with actual room data) fall through to the generic `{"Success":true,"Valid":true}`. These are secondary and don't block the login/launch chain; add explicit handlers where testing reveals the client rejects the fallback.
2. **Room `hot`/`search`/`carousel` handlers return empty `{"Results":[]}`** — the Watch > Play tab will show empty lists. To populate these would require seeding the in-memory room database from the RecFlare capture data (Orientation, IsDorm room objects, etc.). Left as follow-up once the login path is confirmed working.
3. **Server binding** is hardcoded `http://localhost:5229`; the 2023.4 client expects `*.rec.net` hosts redirected (via hosts-file override or the BepInEx plugin pointing to `localhost:5229`, as done for RecFlare).
4. **Photon auth token** is empty — the client cannot connect to Photon Voice/Web after joining a room. RecFlare has the same limitation (Photon tokens null). Required for multiplayer but not for boot/login.

