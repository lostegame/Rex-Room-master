using Rex_Room_20230414.API;
namespace Rex_Room_20230414;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
public static class Server
{
    private static byte[]? _defaultProfileImageBytes;
    private static readonly object _imgSignLock = new();
    private static System.Security.Cryptography.RSA _imgSignRsa;
    private static bool _sigSpecLogged;

    // REC_IMG_SIG is a semicolon-separated spec that varies the locally-generated
    // image signature so we can empirically pin the client's expected format:
    //   ks=<1024|2048>  key size in bits
    //   md=<sha1|sha256> digest
    //   enc=<std|urlsafe|hex> base64 flavor of the signature bytes
    //   pre=<prefix>     literal prefix prepended to the encoded value (e.g. "RSA-SHA1 ")
    private static (int Bits, string Md, string Enc, string Pre) ResolveSigSpec()
    {
        (int Bits, string Md, string Enc, string Pre) res = (2048, "sha1", "std", "");
        string? spec = Environment.GetEnvironmentVariable("REC_IMG_SIG");
        if (string.IsNullOrWhiteSpace(spec)) return res;
        try
        {
            foreach (var kv in spec.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var p = kv.Split('=', 2);
                if (p.Length != 2) continue;
                switch (p[0].Trim().ToLowerInvariant())
                {
                    case "ks" when int.TryParse(p[1].Trim(), out int b) && (b == 1024 || b == 2048): res.Bits = b; break;
                    case "md": res.Md = p[1].Trim().ToLowerInvariant(); break;
                    case "enc": res.Enc = p[1].Trim().ToLowerInvariant(); break;
                    case "pre": res.Pre = p[1].Trim(); break;
                }
            }
        }
        catch { }
        return res;
    }

    private static System.Security.Cryptography.RSA GetImgSignRsa(int bits)
    {
        if (_imgSignRsa == null || _imgSignRsa.KeySize != bits)
        {
            lock (_imgSignLock)
            {
                if (_imgSignRsa == null || _imgSignRsa.KeySize != bits)
                {
                    var old = _imgSignRsa;
                    _imgSignRsa = System.Security.Cryptography.RSA.Create(bits);
                    old?.Dispose();
                }
            }
        }
        return _imgSignRsa;
    }

    private static string BuildLocalImageSignature(byte[] bytes)
    {
        try
        {
            var spec = ResolveSigSpec();
            var rsa = GetImgSignRsa(spec.Bits);
            var hash = spec.Md == "sha256"
                ? System.Security.Cryptography.HashAlgorithmName.SHA256
                : System.Security.Cryptography.HashAlgorithmName.SHA1;
            lock (_imgSignLock)
            {
                byte[] sig = rsa.SignData(bytes, hash, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
                string encoded = spec.Enc switch
                {
                    "urlsafe" => Convert.ToBase64String(sig).Replace('+', '-').Replace('/', '_').TrimEnd('='),
                    "hex" => Convert.ToHexString(sig).ToLowerInvariant(),
                    _ => Convert.ToBase64String(sig)
                };
                if (!_sigSpecLogged)
                {
                    _sigSpecLogged = true;
                    Console.WriteLine($"[img] signature spec: ks={spec.Bits} md={spec.Md} enc={spec.Enc} pre=[{spec.Pre}] rawSigLen={sig.Length} valueLen={spec.Pre.Length + encoded.Length}");
                }
                // Real img.rec.net (and recflare) wrap the base64 signature in a
                // "key-id=KEY:RSA:p1.rec.net; data=<base64>" structure; the client's
                // parser rejects a bare base64 value as "Signature malformed".
                return "key-id=KEY:RSA:p1.rec.net; data=" + spec.Pre + encoded;
            }
        }
        catch
        {
            return "";
        }
    }
    private static volatile bool _haveLiveCreator;
    private static volatile bool _ranProbe;

    private static string ResolveContentPath(string file)
    {
        var candidates = new[]
        {
            System.IO.Path.Combine(AppContext.BaseDirectory, "Content", file),
            System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", file))
        };
        foreach (var c in candidates)
            if (System.IO.File.Exists(c)) return c;
        return candidates[0];
    }

    private static string ResolveLoadingScreenDir()
    {
        var candidates = new[]
        {
            System.IO.Path.Combine(AppContext.BaseDirectory, "loadingscreen"),
            System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "loadingscreen"))
        };
        foreach (var c in candidates)
            if (System.IO.Directory.Exists(c)) return c;
        return candidates[0];
    }

    // Cheap magic-byte check. Player\4\profile.png is a 4,096-byte hand-made stub
    // that is not a decodable image at all - serving it produced a broken avatar,
    // so anything that is not real JPEG/PNG/GIF/BMP data is rejected here and the
    // caller falls through to generated art.
    private static bool LooksLikeImage(byte[] b)
    {
        if (b == null || b.Length < 12) return false;
        // JPEG
        if (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return true;
        // PNG
        if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return true;
        // GIF
        if (b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46) return true;
        // BMP
        if (b[0] == 0x42 && b[1] == 0x4D) return true;
        return false;
    }

    public static byte[]? GetDefaultProfileImageBytes()
    {
        if (_defaultProfileImageBytes != null) return _defaultProfileImageBytes;
        string p = ResolveContentPath("default_profile.jpg");
        if (System.IO.File.Exists(p))
            _defaultProfileImageBytes = System.IO.File.ReadAllBytes(p);
        return _defaultProfileImageBytes;
    }

    private static int _reqIndex = 0;

    private static int NextRequestIndex() => System.Threading.Interlocked.Increment(ref _reqIndex);

    private static readonly List<IApi> ApiList =
    [
        new DefaultApi(),
        new TestApi(),
        new CharadesWordsApi(),
        new AmplitudeApi(),
        new AzureSpeechConfigApi(),
        new PlayerReportingHileApi(),
        new ConfigV2Api(),
        new VersionCheckApi(),
        new GameConfigsApi(),
        new CachedLoginApi(),
        new EacChallengeApi(),
        new AccountMeApi(),
        new ConnectTokenApi(),
        new AvatarApi(),
        new AvatarDefaultApi(),
        new AvatarDefaultBaseApi(),
        new CustomAvatarItemsOwnedApi(),
        new CustomAvatarItemsGrantWornApi(),
        new StorefrontGiftDropStoreApi(),
        new IsCreationAllowedForAccountApi(),
        new IsCreationEnabledApi(),
        new IsRenderingEnabledApi(),
        new SettingsApi(),
        new PlayerLoginApi(),          
        new AccountCreateApi(),        
        new WalletApi(),
        new HeartbeatApi(),
        new AccountBulkApi(),
        new GotoRoomApi(),
        new ForgetPasswordApi(),       
        new ResetPasswordApi(),
        new ClubHomeMeApi(),
        new AvatarItemsApi(),
        new AvatarGiftsGetApi(),
        new PlayerReputationBulkApi(),
        new ProgressionBulkApi(),
        new SubscriptionMemberApi(),
        new SubscriptionDetailsByIdApi(),
        new PageViewConsumeApi(),
        new HubNegotiateApi(),
        new ModerationBlockDetailsApi(),
        new PlayerLogoutApi(),
        new SubscriptionDetailsApi(),
        new GiftDropStoreApi(),
        new EquipmentUnlockedApi(),
        new ConsumablesUnlockedApi(),
        new ChallengeCurrentApi(),
        new MessagesFavoriteStatusApi(),
        new MessagesGetApi(),
        new PlayerEventsAllApi(),
        new QuickPlayApi(),
        new QuickPlayGetAndClearApi(),
        new ExclusiveLoginApi(),
        new RoomKeysRoomApi(),
        new RoomKeysMineApi(),
        new KeepsakesRoomApi(),
        new KeepsakesCategoriesApi(),
        new KeepsakesGlobalConfigApi(),
        new AnnouncementGetApi(),
        new CommunityBoardCurrentApi(),
        new ObjectivesMyProgressApi(),
        new GameRewardsPendingApi(),
        new CampusCardApi(),
        new StorefrontBalanceApi(),
        new RelationshipsGetApi(),
        new RelationshipsFriendsApi(),
        new RelationshipWithApi(),
        new SendFriendRequestApi(),
        new AcceptFriendRequestApi(),
        new RoomFiltersApi(),
        new GotoNoneApi(),
        new MatchmakeRoomApi(),
        new MatchmakeRoomApi("/matchmake/room/{roomId}/{subRoomId}", hasSubRoom: true),
        new MatchmakeRoomApi("/matchmake/v2/room/{roomId}"),
        new MatchmakeRoomApi("/matchmake/v2/room/{roomId}/{subRoomId}", hasSubRoom: true),
        new MatchmakePlayerApi(),
        new MatchmakePlayerApi("/matchmake/v2/player/{playerId}"),
        new PlayerConnectionInfoApi(),
        new PlayerQosApi(),
        new PlayerGameServerRegionPingsApi(),
        new RoomInstancesApi(),
        new ProfileImageApi(),
        new BirthdayApi(),
        new ChangeUsernameApi(),
        new ChangeDisplayNameApi(),
        new ChangePasswordApi(),
        new AccountEmailApi(),
        new AccountPhoneApi(),
        new AccountConfirmPhoneApi(),
        new StatusVisibilityApi(),
        new AvoidJuniorsApi(),
        new PhotonRegionPingsApi(),
        new AvatarSetApi(),
        new AvatarGiftsApi(),
        new AvatarSavedApi(),
        new RoomInstanceReportJoinResultApi(),
        new RoomHotApi(),
        new RoomSearchApi(),
        new RoomCarouselRisingApi(),
        new RoomCreatedByMeApi(),
        new RoomVisitedByMeApi(),
        new RoomFavoritedByMeApi(),
        new RoomCuratedPlaylistsApi(),
        new RoomOwnedByMeApi("/roomserver/rooms/ownedby/{accountId}"),
        new RoomOwnedByMeApi("/rooms/ownedby/{accountId}"),
        new RoomSimilarApi("/roomserver/rooms/{roomId}/similar"),
        new RoomSimilarApi("/rooms/{roomId}/similar"),
        new RoomPlayerDataMeApi(),
        new MatchRoomsRequiringApi(),
        new RoomPhotonAccessTokenApi(),
        new RoomPhotonAccessTokenApi("/roomserver/photon_access_token"),
        new ProgressionEventsActiveApi(),
        new ProgressionEventsEventApi(),
        new ProgressionEventsRecordApi(),
        new RoomServerRoomsApi(),
        new RoomServerRoomsApi("/roomserver/rooms/{roomId}"),
        new RoomServerRoomsSearchApi(),
        new RoomConsumablesApi(),
        new BacktraceConfigApi(),
        new ChallengeV2CurrentApi(),
        new ThreadApi(),
        new AccountProfileImageApi(),
        new SettingsSetApi(),
        new PlayerByIdApi(),
        new ImagesUploadSavedApi(),
        new VoteToKickReasonsApi(),
        new ImagesNamedApi(),
        new RoomCurrenciesBetaApi(),
        new ChatThreadApi(),
        new RoomCurrenciesApi(),
        new RoomCurrenciesAllBalancesApi(),
        new ImagesSlideshowApi(),
        new AnnouncementsUnreadApi(),
        new AnnouncementsSubscriptionUnreadApi(),
        new ClubCategoryTagsApi(),
        new ClubMineCreatedApi(),
        new ClubMineMemberApi(),
        new ClubSearchApi(),
        new ClubhouseSearchMostActiveNowApi(),
        new LoadingScreenTipDataApi(),
        new CachedLoginForPlatformIdsApi(),
        new GotoRoomRecCenterApi(),
        new FeaturedRoomsCurrentApi(),
        new FeaturedRoomsCurrentApi("/roomserver/featuredrooms/current"),
        new NameGenOptionsApi(),
        new AvatarSavedGetApi(),
        new SubscriptionSeasonsApi(),
        new VoiceConfigApi(),
        new CustomAvatarItemsFromCreatorApi(),
        new ImagesV5PlayerApi(),
        new AccountPrivacySettingsApi(),
        new ItemWishlistsMeApi(),
        new ParentalControlMeApi(),
        new MessagesFavoriteFriendOnlineStatusApi(),
        new IncentivizedReferralsProgressApi(),
        new PlatformNotificationsConfigCategoriesApi(),
        new PlatformNotificationsPreferencesApi(),
        new InfluencerPartnerProgramInfluencersApi(),
        new InfluencerPartnerProgramMyInfluencerApi(),
        new InfluencerPartnerProgramInfluencerApi(),
        new SubscriptionSubscriberCountApi(),
        new InventionsFromCreatorsApi(),
        new CustomAvatarItemsV1BulkApi(),
        new RelationshipsMutualFriendsApi(),
        new MessagesSendMultipleApi(),
        new MessagesV3DeleteApi(),
        new RoleDeveloperApi(),
        new ClubCategoryTagsApi(),
        new PurchaseCampaignAllCurrentV2Api(),
        new ExternalFriendInvitePlatformReferrersApi(),
        new PlayerEventsTagFiltersApi(),
        new PlatformNotificationsReceivesGameplayInvitesApi(),
        new RoomServerShowcaseApi(),
        new ItemWishlistsByAccountApi(),
        new RoomsBulkApi("/rooms/bulk"),
        new RoomsBulkApi("/roomserver/rooms/bulk"),
        new RoomsRequiringApi(),
        new AccountBioApi(),
        new GameConfigsV1AllApi(),
        new VoiceConfigApi(),
        new ClubByIdApi(),
        new RoomPlayerDataMeApi(),
        new PlayersAddXpApi(),
        new RoomRecommendationsApi(),
        new RoomInteractionByMeApi(),
        // The /roomserver-prefixed variants of these three are already registered
        // above; only the unprefixed aliases are added here.
        new RoomFavoritedByMeApi("/rooms/favoritedby/me"),
        new RoomVisitedByMeApi("/rooms/visitedby/me"),
        new RoomCreatedByMeApi("/rooms/createdby/me"),

        // --- Room sub-room data (room spawn / InitialRoomLoad) ---
        // GET /rooms/{id}/subrooms/{subId}/data must answer
        // {"success":true,"error":null,"value":{"room":..,"subRoomDataSave":..}}.
        // It used to be swallowed by /rooms/{roomId} and returned a bare room DTO,
        // so the client never received subRoomDataSave and hung on the instance-data
        // download. Registered under both prefixes like the other room routes.
        new RoomSubRoomsDataApi(),
        new RoomSubRoomsDataApi("/roomserver/rooms/{roomId}/subrooms/{subRoomId}/data"),

        // --- Saved images / share camera ---
        // images/v4/room returns an ARRAY of room-image DTOs; v6 resolves one by
        // name (404 unknown); v5/cheered/bulk mirrors the requested ids back.
        new ImagesRoomApi(),
        new ImagesV6Api(),
        new ImagesV5CheeredBulkApi("/api/images/v5/cheered/bulk", HttpMethods.Get),
        // The client also POSTs this one with a urlencoded id=..&id=.. body.
        new ImagesV5CheeredBulkApi("/api/images/v5/cheered/bulk", HttpMethods.Post),
        // ImagesV5PlayerApi is already registered above.

        // --- Watch / events ---
        new PlayerEventsSearchApi("/api/playerevents/v1/search", false),
        new PlayerEventsSearchApi("/api/playerevents/v1/searchlive", true),

        // --- Rec Center inventions ---
        new InventionsApi("/api/inventions/v2/search", "search"),
        new InventionsApi("/api/inventions/v1/featured", "featured"),
        new InventionsApi("/api/inventions/v1/toptoday", "toptoday"),
        new InventionsApi("/api/inventions/v1/version", "version"),
        new InventionsApi("/api/inventions/v1/fromcreators", "fromcreators"),
        new InventionsMineV2Api(),
        new InventionsPurchaseApi("/api/inventions/v1/details", "details"),
        new InventionsPurchaseApi("/api/storefronts/v2/buyInvention", "buy"),
        new InventionsPurchaseApi("/api/inventions/v1/tagfilters", "tagfilters"),

        // --- Last generic-fallback shapes seen in live client traffic ---
        new RemainingShapesApi("/api/images/v1/slideshow", "slideshow"),
        new RemainingShapesApi("/api/objectives/v1/cleargroup", "cleargroup"),
        new RemainingShapesApi("/api/playerevents/v2", "playerevents-create"),
        new RemainingShapesApi("/api/playerevents/v1/{eventId}", "event-detail"),
        new RemainingShapesApi("/api/playerevents/v1/{eventId}/responses", "responses"),
        new RemainingShapesApi("/api/customAvatarItems/v1", "cai-create"),
        new RemainingShapesApi("/api/customAvatarItems/v1/design", "design"),
        new RemainingShapesApi("/api/customAvatarItems/v1/design", "cai-design-save"),

        // --- Telemetry ingest: the client fires these constantly and ignored the
        // --- generic fallback body, but they were logging as false fallbacks.
        new DataCollectionApi("/dc/data/heartbeat"),
        new DataCollectionApi("/dc/data/event"),

        // --- Final four generic fallbacks from the full 299-path replay ---
        new AccountWriteAndMiscApi("/account/me/bio", "bio"),
        new AccountWriteAndMiscApi("/account/me/personalpronouns", "pronouns"),
        new AccountWriteAndMiscApi("/purchase/v1/cleanuppending", "cleanup"),
        new AccountWriteAndMiscApi("/roomserver/playlists", "playlists"),

        // --- Dorm scoreboard ---
        new LeaderboardNearbyScoresApi(),

        // --- Custom avatar items ---
        new CustomAvatarItemsApi("/api/customAvatarItems/v1/featured", "featured"),
        new CustomAvatarItemsApi("/api/customAvatarItems/v1/hot", "hot"),
        new CustomAvatarItemsApi("/api/customAvatarItems/v1/minPriceForPublicItem", "minPrice"),

        // --- Storefront ---
        new StorefrontApi("/api/storefronts/v1/adcarouselitems", "adcarousel"),
        new StorefrontApi("/api/storefronts/v1/balanceAddType/2/{balanceId}", "balanceAddType"),
        new StorefrontApi("/api/storefronts/v2/buyItem", "buyItem"),

        // The bag. Registered next to buyItem because the reference treats them as
        // the same purchase path; this one is what a player-to-player gift posts to,
        // and it was missing entirely, so gifts never left the sender.
        new BulkPurchaseApi(),

        // --- Misc single-purpose routes ---
        new MiscApi("/showcase/{accountId}", "showcase"),
        new MiscApi("/reminder/currentTokenBundles/v2", "reminder"),
        new MiscApi("/api/checklist/v1/complete", "checklist"),
        new MiscApi("/api/consumables/v1/consume", "consume"),
        new MiscApi("/api/sanitize/v1", "sanitize"),
        new MiscApi("/actionlink", "actionlink"),

        // Room interactions: cheer (like) + favorite toggles. Reference rooms.app.ts
        // §24-27. Registered under BOTH the /roomserver prefix (how the 2023 client
        // reaches them, via the discovery Rooms base) and the unprefixed path.
        new RoomInteractionByMeApi("/roomserver/rooms/{roomId}/interactionby/me/cheer", "cheer-toggle"),
        new RoomInteractionByMeApi("/roomserver/rooms/{roomId}/interactionby/me/favorite", "favorite-toggle"),
        // The plain GET. Needed explicitly: MatchScore gives no penalty for extra
        // path segments, so "/rooms/{roomId}" (the room DTO route) outscored the
        // default "/roomserver/rooms/{roomId}/interactionby/me" for an unprefixed
        // request and answered the interaction read with a room DTO.
        new RoomInteractionByMeApi("/rooms/{roomId}/interactionby/me", "read"),
        new RoomInteractionByMeApi("/rooms/{roomId}/interactionby/me/cheer", "cheer-toggle"),
        new RoomInteractionByMeApi("/rooms/{roomId}/interactionby/me/favorite", "favorite-toggle"),
        new RoomInteractionByMeApi("/rooms/{roomId}/interactionby/me/cheer", "cheer-remove"),
        new RoomInteractionByMeApi("/rooms/{roomId}/interactionby/me/favorite", "favorite-remove"),
        new RoomInteractionByMeApi("/roomserver/rooms/{roomId}/interactionby/me/cheer", "cheer-remove"),
        new RoomInteractionByMeApi("/roomserver/rooms/{roomId}/interactionby/me/favorite", "favorite-remove")
    ];
    
    public static async Task StartAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls("https://localhost:5229");
        builder.WebHost.ConfigureKestrel(k => k.AllowSynchronousIO = true);
        var app = builder.Build();
        app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(15) });

        // Pre-create the Dorm game room on Luxon (Photon MasterServer) so the
        // real client's JoinGame(CreateIfNotExists=false) finds it. The room must
        // exist at the exact moment the client joins, so we keep ONE authenticated
        // connection alive permanently. Luxon deletes an empty room immediately
        // (empty_game_ttl=0), but a live connected creator counts as a peer in the
        // room, so the room stays up the whole time. If that connection dies, we
        // reconnect and re-create.
        //
        // On the Official Photon Cloud wiring (default) there is NO local Luxon:
        // the client creates/joins its rooms itself on Photon Cloud, so this loop
        // is disabled unless REXROOM_PRECREATE_DORM=1 is set for a Luxon setup.
        bool preCreateDorm = string.Equals(Environment.GetEnvironmentVariable("REXROOM_PRECREATE_DORM"), "1", StringComparison.OrdinalIgnoreCase);
        _ = Task.Run(async () =>
        {
            if (!preCreateDorm)
            {
                Console.WriteLine("[PhotonGameCreator] local Luxon pre-create disabled (REXROOM_PRECREATE_DORM unset) — client creates rooms on Official Photon Cloud.");
                return;
            }
            bool probeEnabled = string.Equals(Environment.GetEnvironmentVariable("REXROOM_PHOTON_PROBE_JOIN"), "1", StringComparison.OrdinalIgnoreCase);
            while (true)
            {
                try
                {
                    if (!_haveLiveCreator)
                    {
                        var pc = PhotonGameCreator.EnsureDormReady();
                        Console.WriteLine($"[PhotonGameCreator] {pc.Message}");
                        if (pc.Success && pc.LivePeer != null)
                        {
                            _haveLiveCreator = true;
                            var live = pc.LivePeer;
                            _ = Task.Run(() =>
                            {
                                try { live.KeepAliveLoop(TimeSpan.FromMilliseconds(500)); }
                                catch { }
                                finally { _haveLiveCreator = false; }
                            });
                        }
                    }

                    if (probeEnabled && !_ranProbe)
                    {
                        _ranProbe = true;
                        _ = Task.Run(() =>
                        {
                            // STEP 1 oracle: create-if-exists in the DEFAULT "" lobby.
                            // If rc=32766 (already-exists) -> games.contains HIT for this id.
                            // ProbeJoin('') rc=32758 earlier -> games.find MISS for same id/lobby.
                            var o1 = PhotonGameCreator.ProbeCreateExisting(PhotonGameCreator.GameId, "");
                            Console.WriteLine($"[PhotonGameCreator] ORACLE(default-lobby contains): {o1.Message}");

                            // STEP 2: create the SAME GameId inside a NAMED lobby 'default'.
                            var c2 = PhotonGameCreator.CreateGame("default");
                            Console.WriteLine($"[PhotonGameCreator] create-in-default-lobby: {c2.Message}");
                            if (c2.Success)
                            {
                                var o2 = PhotonGameCreator.ProbeCreateExisting(PhotonGameCreator.GameId, "default");
                                Console.WriteLine($"[PhotonGameCreator] ORACLE(default-lobby contains): {o2.Message}");
                            }

                            foreach (var lobby in new[] { "default", "" })
                            {
                                var r = PhotonGameCreator.ProbeJoin(lobby);
                                Console.WriteLine($"[PhotonGameCreator] PROBE: {r.Message}");
                            }

                            // DECISIVE: create a brand-new room with a unique id and
                            // join+recreate it on the SAME connection. If join of a
                            // freshly-created room works (rc=0), the server bug only
                            // affects long-lived kept-alive rooms. If it also fails
                            // (rc=32758), JoinGame lookup on a live room is broken.
                            var freshId = "probe-" + DateTime.UtcNow.Ticks.ToString("x");
                            var sc = PhotonGameCreator.ProbeCreateThenJoinSameConn(freshId, "");
                            Console.WriteLine($"[PhotonGameCreator] SAME-CONN: {sc.Message}");

                            // If CreateIfNotExists=true join succeeds, the real
                            // RecRoom client (which often sends that flag) can
                            // join — and we know a server patch / proxy answer
                            // for op 226 can just flip this flag.
                            var freshId2 = "probe-" + DateTime.UtcNow.Ticks.ToString("x");
                            var fc = PhotonGameCreator.ProbeCreateThenJoinCreateFlag(freshId2, "");
                            Console.WriteLine($"[PhotonGameCreator] FLAG-CONN: {fc.Message}");

                            // Sweep candidate parameter keys for CreateIfNotExists.
                            // A correct key will make JoinGame create the room
                            // instead of returning 32758.
                            var freshId3 = "probe-" + DateTime.UtcNow.Ticks.ToString("x");
                            var sw = PhotonGameCreator.ProbeJoinSweepKeys(freshId3, "");
                            Console.WriteLine($"[PhotonGameCreator] SWEEP: {sw.Message}");

                            // JoinGame with an EMPTY room id + CreateIfNotExists=1
                            // across 200..254: the winning key makes the server
                            // mint a new room (rc=0) instead of 32758.
                            var esw = PhotonGameCreator.ProbeJoinEmptyIdFlagSweep();
                            Console.WriteLine($"[PhotonGameCreator] EMPTYID-SWEEP: {esw.Message}");

                            var fny = "probe-" + DateTime.UtcNow.Ticks.ToString("x");
                            var fn = PhotonGameCreator.ProbeJoinFlagNoCreate(fny, "");
                            Console.WriteLine($"[PhotonGameCreator] FLAGNO-YET: {fn.Message}");

                            var dm = PhotonGameCreator.ProbeJoinDormFlag(PhotonGameCreator.GameId, "");
                            Console.WriteLine($"[PhotonGameCreator] JOINDORM-FLAG: {dm.Message}");

                            var twoLegId = "probe-2leg-" + DateTime.UtcNow.Ticks.ToString("x");
                            var tl = PhotonGameCreator.ProbeSecondLegGameServer(twoLegId, "");
                            Console.WriteLine($"[PhotonGameCreator] SECOND-LEG: {tl.Message}");
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PhotonGameCreator] error: {ex.GetType().Name}: {ex.Message}");
                }
                try { await Task.Delay(TimeSpan.FromSeconds(15)); }
                catch { break; }
            }
        });
        
        app.Run(async context =>
        {
            try
            {
            var path = context.Request.Path;
            var method = context.Request.Method;
            try { System.IO.File.AppendAllText(@"C:\Users\LOSTEG~1\AppData\Local\Temp\opencode\rexroom_req.log", DateTime.UtcNow.ToString("HH:mm:ss.fff") + " " + method + " " + path + context.Request.QueryString + Environment.NewLine); } catch { }
            
            if (path == "/hub/v1" && context.WebSockets.IsWebSocketRequest)
            {
                Console.WriteLine($"[WebSocket] Connection at {path}");
                var ws = await context.WebSockets.AcceptWebSocketAsync();
                await HandleSignalRConnection(ws, context.Request.Query);
                return;
            }
            
            int reqIndex = NextRequestIndex();
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Console.WriteLine($"[Request] #{reqIndex} {method} {path}");
            try { System.IO.File.AppendAllText(@"C:\Users\LOSTEG~1\AppData\Local\Temp\opencode\rexroom_requests.log", $"{DateTime.Now:HH:mm:ss.fff} #{reqIndex} {method} {context.Request.Path}{context.Request.QueryString}" + Environment.NewLine); } catch { }

            if (path.StartsWithSegments("/storage/profileimage") && method == "GET")
            {
                var pathStr = path.Value ?? "";
                var segments = pathStr.Split('/');
                if (segments.Length >= 4 && int.TryParse(segments[^1], out int imgAccountId))
                {
                    var imgData = Database.DatabaseManager.GetProfileImage(imgAccountId);
                    if (imgData != null)
                    {
                        context.Response.ContentType = "image/png";
                        await context.Response.Body.WriteAsync(imgData, 0, imgData.Length);
                        return;
                    }
                }
                          context.Response.StatusCode = 404;
                          await context.Response.WriteAsync("{\"error\":\"Not found\"}");
                          return;
            }

            // GET /account/{id}/bio used to be short-circuited here with a hardcoded
            // "Hello!" for every account, which ran BEFORE the ApiList dispatch and
            // so shadowed AccountBioApi - a bio written via PUT /account/me/bio read
            // straight back as "Hello!" no matter what was stored. Removed so the
            // real AccountBioApi handler answers with the persisted value.

            if (method == "GET")
            {
                string proxyHost = null;
                bool isImageProxy = false;
                if (path.StartsWithSegments("/img")) { proxyHost = "https://img.rec.net"; isImageProxy = true; }
                else if (path.StartsWithSegments("/data")) proxyHost = "http://eq-cdn.gabethefirst.com";
                else if (path.StartsWithSegments("/room") && !IsRoomInstancesPath(path)) proxyHost = "http://eq-cdn.gabethefirst.com";

                if (proxyHost != null)
                {
                    var proxyMsg = $"[PROXY] GET {path.Value}{context.Request.QueryString} -> {proxyHost}";
                    Console.WriteLine(proxyMsg);
                    try { System.IO.File.AppendAllText(@"C:\Users\LOSTEG~1\AppData\Local\Temp\opencode\rexroom_proxy.log", DateTime.UtcNow.ToString("HH:mm:ss.fff") + " " + proxyMsg + Environment.NewLine); } catch { }
                    if (isImageProxy)
                    {
                        // Image proxy to the official RecRoom image CDN (img.rec.net).
                        // The client verifies an RSA-SHA1 signature over the bytes using
                        // the "Content-Signature" response header. The only way to make
                        // that check pass is to forward the UPSTREAM signature untouched;
                        // locally-generated images (or the local default_profile.jpg)
                        // would fail verification and crash the client. So we pass the
                        // body AND the original Content-Signature through verbatim, and
                        // skip the signature check entirely for clients that query
                        // img.rec.net URIs that do not exist (returning 404 instead of
                        // an unsigned 200).
                        string decodedPath = Uri.UnescapeDataString(path.Value ?? "");
                        string imgPath = decodedPath;
                        if (imgPath.StartsWith("/img/", StringComparison.OrdinalIgnoreCase))
                            imgPath = imgPath.Substring(5);
                        else if (imgPath.Equals("/img", StringComparison.OrdinalIgnoreCase))
                            imgPath = "";

                        // Players without a custom image are assigned
                        // ProfileImage = "DefaultProfileImage.jpg" (AccountCreateApi).
                        // img.rec.net does not host this file, and the client's RSA
                        // signature check is disabled via the RecNetPlugin
                        // ("Disable Signature Verification"), so we can serve the
                        // bundled default image locally instead of proxying to a CDN.
                        bool isDefaultProfileRequest =
                            imgPath.Equals("DefaultProfileImage", StringComparison.OrdinalIgnoreCase) ||
                            imgPath.Equals("DefaultProfileImage.jpg", StringComparison.OrdinalIgnoreCase);
                        if (isDefaultProfileRequest)
                        {
                            var defaultBytes = GetDefaultProfileImageBytes();
                            if (defaultBytes != null)
                            {
                                context.Response.ContentType = "image/jpeg";
                                var defSig = BuildLocalImageSignature(defaultBytes);
                                if (!string.IsNullOrEmpty(defSig))
                                    context.Response.Headers["Content-Signature"] = defSig;
                                Console.WriteLine($"[imgProxy] serving local default profile image");
                                await context.Response.Body.WriteAsync(defaultBytes, 0, defaultBytes.Length);
                                return;
                            }
                        }

                        // Loading-screen backgrounds shipped in the "loadingscreen"
                        // folder are served locally (never proxied to the CDN), so
                        // the game shows our images instead of real Rec Room assets.
                        // RSA signature verification is disabled via the RecNetPlugin
                        // ("Disable Signature Verification"), so unsigned local files
                        // are accepted by the client. The requested name is the one
                        // advertised in /config/LoadingScreenTipData as "ImageName".
                        string lsDir = ResolveLoadingScreenDir();
                        string lsFileName = System.IO.Path.GetFileName(Uri.UnescapeDataString(imgPath));
                        if (!string.IsNullOrEmpty(lsFileName))
                        {
                            string lsFile = System.IO.Path.Combine(lsDir, lsFileName);
                            if (System.IO.File.Exists(lsFile))
                            {
                                string lsExt = System.IO.Path.GetExtension(lsFile).ToLowerInvariant();
                                context.Response.ContentType =
                                    lsExt == ".png" ? "image/png" :
                                    lsExt == ".webp" ? "image/webp" :
                                    lsExt == ".bmp" ? "image/bmp" :
                                    "image/jpeg";
                                byte[] lsBytes = await System.IO.File.ReadAllBytesAsync(lsFile);
                                var lsSig = BuildLocalImageSignature(lsBytes);
                                if (!string.IsNullOrEmpty(lsSig))
                                    context.Response.Headers["Content-Signature"] = lsSig;
                                Console.WriteLine($"[imgProxy] serving local loading screen image {lsFileName}");
                                await context.Response.Body.WriteAsync(lsBytes);
                                return;
                            }
                        }

                        string targetUrl = proxyHost + "/" + imgPath + context.Request.QueryString;
                        Console.WriteLine($"[imgProxy] GET {targetUrl}");

                        try
                        {
                            using var proxyHandler = new HttpClientHandler
                            {
                                ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
                            };
                            using var proxyClient = new HttpClient(proxyHandler);
                            proxyClient.Timeout = TimeSpan.FromSeconds(4);
                            using var proxyResp = await proxyClient.GetAsync(targetUrl);
                            if (proxyResp.IsSuccessStatusCode)
                            {
                                byte[] imgBytes = await proxyResp.Content.ReadAsByteArrayAsync();
                                if (imgBytes.Length > 0)
                                {
                                    var upstreamSig = proxyResp.Headers.TryGetValues("Content-Signature", out var sigs)
                                        ? string.Join(",", sigs)
                                        : null;
                                    if (!string.IsNullOrEmpty(upstreamSig))
                                        context.Response.Headers["Content-Signature"] = upstreamSig;

                                    var ct = proxyResp.Content.Headers.ContentType;
                                    context.Response.ContentType = string.IsNullOrEmpty(ct?.ToString())
                                        ? "application/octet-stream"
                                        : ct.ToString();
                                    Console.WriteLine($"[imgProxy] OK {targetUrl} bytes={imgBytes.Length} sig={(string.IsNullOrEmpty(upstreamSig) ? "MISSING" : "OK")}");
                                    await context.Response.Body.WriteAsync(imgBytes, 0, imgBytes.Length);
                                    return;
                                }
                            }
                        }
                        catch (Exception proxyEx)
                        {
                            Console.WriteLine($"[imgProxy] error: {proxyEx.Message}");
                        }

                        // Upstream image missing / unreachable / unsigned-empty.
                        // Profile-image requests (/img/profile/...jpg) — which is
                        // how the client renders every account avatar — fall back
                        // to the locally-stored upload for the matching account
                        // when one exists, otherwise to the bundled default
                        // profile image. Without this, accounts whose ProfileImage
                        // hash exists only on this server (it never touched
                        // img.rec.net) are served a bare 404 and render no avatar.
                        bool isProfileImageRequest =
                            imgPath.StartsWith("profile/", StringComparison.OrdinalIgnoreCase);
                        if (isProfileImageRequest)
                        {
                            byte[] localImg = null;
                            foreach (var acc in Database.DatabaseManager.LoadAllPlayerData())
                            {
                                if (string.IsNullOrEmpty(acc.ProfileImage)) continue;
                                if (string.Equals(acc.ProfileImage.Replace("\\", "/"), imgPath, StringComparison.OrdinalIgnoreCase))
                                {
                                    localImg = Database.DatabaseManager.GetProfileImage(acc.Id);
                                    if (localImg != null && LooksLikeImage(localImg)) break;
                                    localImg = null;
                                }
                            }

                            // Exact-name match only. A stale or differently-named
                            // ProfileImage used to fall through to the shared
                            // default avatar, which is indistinguishable from
                            // "every avatar is the same picture". Falling through
                            // to the per-name generator instead gives that player
                            // their own tile instead of everyone else's face.
                            if (localImg == null)
                            {
                                byte[] generated = API.PlaceholderImageFactory.Get(imgPath);
                                if (generated != null)
                                {
                                    context.Response.ContentType = "image/png";
                                    var genSig = BuildLocalImageSignature(generated);
                                    if (!string.IsNullOrEmpty(genSig))
                                        context.Response.Headers["Content-Signature"] = genSig;
                                    Console.WriteLine($"[imgProxy] serving generated avatar placeholder for {imgPath}");
                                    await context.Response.Body.WriteAsync(generated, 0, generated.Length);
                                    return;
                                }
                            }

                            if (localImg == null)
                                localImg = GetDefaultProfileImageBytes();
                            if (localImg != null)
                            {
                                context.Response.ContentType = "image/jpeg";
                                var fallbackSig = BuildLocalImageSignature(localImg);
                                if (!string.IsNullOrEmpty(fallbackSig))
                                    context.Response.Headers["Content-Signature"] = fallbackSig;
                                Console.WriteLine($"[imgProxy] serving local profile image (fallback) {imgPath}");
                                await context.Response.Body.WriteAsync(localImg, 0, localImg.Length);
                                return;
                            }
                        }

                        // Saved "sharecamera" photos. The store persisted only the
                        // name before, so /img/sharecamera/... had nothing behind it
                        // and every photo a player took rendered as the shared
                        // default image. Serve the real uploaded bytes first.
                        if (imgPath.StartsWith("sharecamera/", StringComparison.OrdinalIgnoreCase))
                        {
                            byte[] savedBytes = API.SavedImageStore.ReadBytesByName(imgPath);
                            if (savedBytes != null && savedBytes.Length > 0)
                            {
                                context.Response.ContentType = LooksLikeImage(savedBytes) ? "image/jpeg" : "image/png";
                                var savedSig = BuildLocalImageSignature(savedBytes);
                                if (!string.IsNullOrEmpty(savedSig))
                                    context.Response.Headers["Content-Signature"] = savedSig;
                                Console.WriteLine($"[imgProxy] serving saved image bytes for {imgPath} ({savedBytes.Length} bytes)");
                                await context.Response.Body.WriteAsync(savedBytes, 0, savedBytes.Length);
                                return;
                            }
                        }

                        // Any other missing image (room thumbnails, invention
                        // thumbnails, event art) used to 404 and rendered as a
                        // broken/blank tile in the client.
                        //
                        // It then "served the bundled default image", which is
                        // Content/default_profile.jpg - so EVERY tile in the game
                        // rendered as the same default avatar. That is why room
                        // tiles, invention tiles and shop art all looked identical.
                        // img.rec.net is gone from DNS, so there is no real art to
                        // fall back on either.
                        //
                        // Now we synthesise a stable image per requested name, so
                        // each room/invention/event gets its own distinguishable
                        // tile with its own label. RSA verification is disabled
                        // client-side, so a locally signed image is accepted.
                        {
                            byte[] generated = API.PlaceholderImageFactory.Get(imgPath);
                            if (generated != null)
                            {
                                context.Response.ContentType = "image/png";
                                var genSig = BuildLocalImageSignature(generated);
                                if (!string.IsNullOrEmpty(genSig))
                                    context.Response.Headers["Content-Signature"] = genSig;
                                Console.WriteLine($"[imgProxy] serving generated placeholder for {imgPath} ({generated.Length} bytes)");
                                await context.Response.Body.WriteAsync(generated, 0, generated.Length);
                                return;
                            }

                            // Generator failed - last resort is the bundled file.
                            byte[] placeholder = GetDefaultProfileImageBytes();
                            if (placeholder != null)
                            {
                                context.Response.ContentType = "image/jpeg";
                                var phSig = BuildLocalImageSignature(placeholder);
                                if (!string.IsNullOrEmpty(phSig))
                                    context.Response.Headers["Content-Signature"] = phSig;
                                Console.WriteLine($"[imgProxy] serving bundled default image (generator failed) {imgPath}");
                                await context.Response.Body.WriteAsync(placeholder, 0, placeholder.Length);
                                return;
                            }
                        }
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("{\"error\":\"Not found\"}");
                        return;
                    }

                    try
                    {
                        var proxyReq = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(proxyHost + context.Request.Path + context.Request.QueryString);
                        proxyReq.Method = "GET";
                        proxyReq.Timeout = 4000;
                        using var proxyResp = (System.Net.HttpWebResponse)proxyReq.GetResponse();
                        using var proxyStream = proxyResp.GetResponseStream();
                        using var ms = new System.IO.MemoryStream();
                        proxyStream.CopyTo(ms);
                        if (ms.Length > 0)
                        {
                            var ct = proxyResp.ContentType;
                            context.Response.ContentType = string.IsNullOrEmpty(ct) ? "application/octet-stream" : ct;
                            await context.Response.Body.WriteAsync(ms.ToArray(), 0, (int)ms.Length);
                            return;
                        }
                    }
                    catch { }

                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("{\"error\":\"Not found\"}");
                    return;
                }
            }

            if (path == "/" && method == "GET")
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(@"{""Auth"":""https://localhost:5229/"",""API"":""https://localhost:5229/"",""WWW"":""https://localhost:5229/"",""Notifications"":""https://localhost:5229/"",""Images"":""https://localhost:5229/img"",""CDN"":""https://localhost:5229/"",""Commerce"":""https://localhost:5229/"",""Matchmaking"":""https://localhost:5229/"",""Storage"":""https://localhost:5229/"",""Chat"":""https://localhost:5229/chat"",""Leaderboard"":""https://localhost:5229/"",""Accounts"":""https://localhost:5229/"",""Link"":""https://localhost:5229/"",""RoomComments"":""https://localhost:5229/"",""Clubs"":""https://localhost:5229/"",""Rooms"":""https://localhost:5229/roomserver"",""PlatformNotifications"":""https://localhost:5229/pn"",""Moderation"":""https://localhost:5229/mo"",""DataCollection"":""https://localhost:5229/dc"",""BugReporting"":""https://localhost:5229/br"",""Discovery"":""https://localhost:5229/disc"",""Econ"":""https://localhost:5229/"",""CMS"":""https://localhost:5229/cms"",""GameLogs"":""https://localhost:5229/gl"",""Lists"":""https://localhost:5229/li"",""PlayerSettings"":""https://localhost:5229/"",""Strings"":""https://localhost:5229/s"",""StringsCDN"":""https://localhost:5229/scdn"",""Studio"":""https://localhost:5229/st"",""AI"":""https://localhost:5229/"",""Cards"":""https://localhost:5229/"",""Data"":""https://localhost:5229/"",""Geo"":""https://localhost:5229/"",""RoomieIntegrations"":""https://localhost:5229/"",""Thorn"":""https://localhost:5229/"",""Videos"":""https://localhost:5229/""}");
                return;
            }

            if (path.Value!.StartsWith("/sigs/") && method == "GET")
            {
                // The client's FileHashChecker (BLGELNMKAKM::CheckHashesInBackground)
                // downloads this file and parses it as a hash manifest: one line per
                // game file, formatted "relative\\path|hash". A line without '|'
                // (e.g. the old placeholder text) makes the split yield one element,
                // indexing [1] throws IndexOutOfRangeException, and the client exits
                // ("File check failed").
                //
                // The stock verification.sig on this machine is from a different
                // game build (0/3000 paths exist locally), so we rebuild the
                // manifest from the files that are actually present, using the same
                // path|hash shape. Every line then parses AND every hash matches its
                // own local file, so the hash check passes instead of aborting.
                string manifest = BuildVerificationManifest();
                if (!string.IsNullOrEmpty(manifest))
                {
                    var body = System.Text.Encoding.UTF8.GetBytes(manifest);
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/octet-stream";
                    Console.WriteLine($"[sigs] serving generated manifest bytes={body.Length}");
                    await context.Response.Body.WriteAsync(body, 0, body.Length);
                    return;
                }

                // No local files were enumerable: an empty 200 is parseable
                // (zero lines) and avoids the crash; a text placeholder is not.
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/octet-stream";
                await context.Response.WriteAsync("");
                return;
            }

            IApi best = null;
            int bestScore = 0;

            foreach (IApi api in ApiList)
            {
                // Kestrel hands back the method verbatim, so compare case-insensitively:
                // otherwise a client sending "Get" matches no handler and silently
                // degrades into the generic fallback.
                if (!string.Equals(method, api.GetMethod(), StringComparison.OrdinalIgnoreCase)) continue;
                int score = MatchScore(path.Value ?? "", api.GetUrl());
                if (score > bestScore)
                {
                    bestScore = score;
                    best = api;
                }
            }

            if (best != null)
            {
                context.Response.ContentType = "application/json";
                string respBody;
                try
                {
                    respBody = best.Response(context);
                    int statusCode2 = best.GetStatusCode();
                    try { System.IO.File.AppendAllText(@"C:\Users\LOSTEG~1\AppData\Local\Temp\opencode\rexroom_requests.log",
                        $"{DateTime.Now:HH:mm:ss.fff} #{reqIndex} {method} {path} => [{statusCode2}] len={respBody.Length} {respBody}" + Environment.NewLine); } catch { }
                }
                catch (Exception ex)
                {
                    respBody = $"{{\"Success\":false,\"Error\":\"{ex.GetType().Name}: {ex.Message.Replace("\"", "\\\"")}\"}}";
                    Console.WriteLine($"[EXCEPTION] {method} {path} => {ex.GetType().Name}: {ex.Message}");
                    Console.WriteLine(ex.ToString());
                    try {
                        System.IO.File.AppendAllText(@"C:\Users\LOSTEG~1\AppData\Local\Temp\opencode\rexroom_errors.log", $"{DateTime.Now:HH:mm:ss.fff} {method} {path} => {ex}\r\n\r\n");
                        System.IO.File.AppendAllText(@"C:\Users\LOSTEG~1\AppData\Local\Temp\opencode\rexroom_requests.log", $"{DateTime.Now:HH:mm:ss.fff} #{reqIndex} {method} {path} => [500] EXCEPTION {ex.GetType().Name}: {ex.Message}" + Environment.NewLine);
                    } catch { }
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(respBody);
                    return;
                }
                int statusCode = best.GetStatusCode();
                if (statusCode != 200) context.Response.StatusCode = statusCode;
                // 204/304 must not carry a body; Kestrel throws on WriteAsync for them.
                if (statusCode != 204 && statusCode != 304)
                    await context.Response.WriteAsync(respBody);
                Console.WriteLine($"[RESP {statusCode}] #{reqIndex} {method} {path} => {respBody.Substring(0, Math.Min(400, respBody.Length))}");
                TrafficRecorder.Record(reqIndex, method, path, statusCode, respBody,
                    best.GetType().Name + " " + best.GetUrl());
            }
            else
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
                Console.WriteLine($"[FALLBACK] {method} {path}");
                TrafficRecorder.NoteFallback(path);
                try { System.IO.File.AppendAllText(@"C:\Users\LOSTEG~1\AppData\Local\Temp\opencode\rexroom_fallback.log", $"{DateTime.Now:HH:mm:ss.fff} {method} {path} => FALLBACK(Success/Valid=true)" + Environment.NewLine); } catch { }
                await context.Response.WriteAsync(@"{""Success"": true, ""Valid"": true}");
                TrafficRecorder.Record(reqIndex, method, path, 200, @"{""Success"": true, ""Valid"": true}", "(none - generic fallback)");
            }
            }
            catch (Exception ex)
            {
                // Belt-and-suspenders: catch anything that escaped the endpoint-level try/catch.
                try {
                    System.IO.File.AppendAllText(@"C:\Users\LOSTEG~1\AppData\Local\Temp\opencode\rexroom_errors.log", $"{DateTime.Now:HH:mm:ss.fff} {context.Request.Method} {context.Request.Path} => {ex}\r\n\r\n");
                } catch { }
                Console.WriteLine($"[RUN-CRASH-GUARD] {context.Request.Method} {context.Request.Path} => {ex.GetType().Name}: {ex.Message}");
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync($"{{\"Success\":false,\"Error\":\"{ex.GetType().Name}: {ex.Message.Replace("\"", "\\\"")}\"}}");
                }
            }
        });
        await app.RunAsync();
    }

    // Route scoring: longest / most-specific match wins so that static routes
    // (e.g. /rooms/hot) beat wildcard prefix handlers (e.g. /rooms/{id}).
    // A {segment} in GetUrl() matches any single path segment; otherwise the
    // url must be a prefix of the path (trailing dynamic catch-alls).
    private static bool IsRoomInstancesPath(PathString path)
        => System.Text.RegularExpressions.Regex.IsMatch(path.Value ?? "",
            @"^/room/[0-9]+/instances$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static int MatchScore(string path, string url)
    {
        if (string.IsNullOrEmpty(url)) return 0;
        var p = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var u = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (u.Length == 0) return 0;

        int matched = 0;
        int staticSegments = 0;
        for (int i = 0; i < u.Length; i++)
        {
            string us = u[i];
            if (us.Length >= 2 && us[0] == '{' && us[^1] == '}')
            {
                if (i >= p.Length) return 0;
                matched++;
                continue;
            }
            if (i >= p.Length || !string.Equals(us, p[i], StringComparison.OrdinalIgnoreCase))
                return 0;
            matched++;
            staticSegments++;
        }

        // Static segments are strictly more specific than {wildcard} ones, so
        // /rooms/bulk must outrank /rooms/{roomId} regardless of ApiList order.
        int score = matched + staticSegments * 3;
        if (p.Length == u.Length) score += 10; // exact path length bonus
        return score;
    }

    private static readonly byte[] RecordSep = new byte[] { 0x1E };
    private static readonly byte[] HandshakeResponse = Encoding.UTF8.GetBytes("{}\x1E");
    private static readonly byte[] PingResponse = Encoding.UTF8.GetBytes("{\"type\":6}\x1E");

    private static async Task HandleSignalRConnection(WebSocket ws, IQueryCollection query)
    {
        var buffer = new byte[8192];
        var msgBuffer = new StringBuilder();
        var cts = new CancellationTokenSource();
        var connectionId = query.ContainsKey("id") ? query["id"].ToString() : "unknown";
        Console.WriteLine($"[SignalR] WebSocket opened for connection {connectionId}");
        
        try
        {
            // إرسال Ping دوري (كل 10 ثوانٍ) للحفاظ على الاتصال مفتوحاً
            var pingTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    await Task.Delay(10000, cts.Token);
                    if (ws.State == WebSocketState.Open)
                    {
                        try
                        {
                            var pingMessage = Encoding.UTF8.GetBytes("{\"type\":6}\x1E");
                            await ws.SendAsync(new ArraySegment<byte>(pingMessage), WebSocketMessageType.Text, true, cts.Token);
                            Console.WriteLine($"[SignalR] Sent ping to {connectionId}");
                        }
                        catch { break; }
                    }
                }
            });

            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    msgBuffer.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    
                    if (result.EndOfMessage)
                    {
                        var msg = msgBuffer.ToString().TrimEnd((char)0x1E);
                        msgBuffer.Clear();
                        
                        if (msg.Length > 2 && !msg.Contains("\"type\":6"))
                            Console.WriteLine($"[SignalR] Recv from {connectionId}: {msg.Substring(0, Math.Min(120, msg.Length))}");

                        // رسالة البروتوكول (تحدث أولاً)
                        if (msg.Contains("\"protocol\""))
                        {
                            await ws.SendAsync(HandshakeResponse, WebSocketMessageType.Text, true, CancellationToken.None);
                            Console.WriteLine($"[SignalR] Sent handshake response to {connectionId}");
                        }
                        // Ping (نوع 6) - نرسل PingResponse أيضاً لكن اللعبة قد ترسل Ping أيضاً
                        else if (msg.Contains("\"type\":6"))
                        {
                            await ws.SendAsync(PingResponse, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        // استدعاء (نوع 1) - نرد بنتيجة فارغة
                        else if (msg.Contains("\"type\":1"))
                        {
                            string invocationId = "0";
                            try
                            {
                                using var doc = System.Text.Json.JsonDocument.Parse(msg);
                                if (doc.RootElement.TryGetProperty("invocationId", out var id))
                                    invocationId = id.GetString() ?? "0";
                            }
                            catch { }
                            
                            var completion = $"{{\"type\":3,\"invocationId\":\"{invocationId.Replace("\"", "\\\"")}\",\"result\":null}}\x1E";
                            await ws.SendAsync(Encoding.UTF8.GetBytes(completion), WebSocketMessageType.Text, true, CancellationToken.None);
                            Console.WriteLine($"[SignalR] Sent completion for invocation {invocationId} to {connectionId}");
                        }
                    }
                }
            }
            cts.Cancel();
            await pingTask;
        }
        catch { }
        finally { cts.Cancel(); }
        
        if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
        {
            try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None); } catch { }
        }
        Console.WriteLine($"[SignalR] WebSocket closed for connection {connectionId}");
    }

    private static string _sigManifestCache;
    private static DateTime _sigManifestCacheAt = DateTime.MinValue;

    // Builds a well-formed hash manifest for /sigs/<guid>.
    //
    // The real game's verification.sig is one line per game file:
    //     "relative\\path|256-byte RSA signature (base64, 344 chars)"
    // The client's FileHashChecker (BLGELNMKAKM::CheckHashesInBackground)
    // downloads it and splits every line at '|'. A line without a '|'
    // produces a single-element array; indexing [1] throws
    // IndexOutOfRangeException and the client exits with "File check failed".
    //
    // RSA verification itself is skipped at runtime (RecNetPlugin forces
    // VerifyData/VerifyHash to true), so the only thing that matters here is
    // that every line PARSES. We enumerate the files that actually exist in
    // the game folder and emit "path|" + a fixed-length placeholder of the
    // same 344-char shape, so all lines split cleanly.
    private static string BuildVerificationManifest()
    {
        try
        {
            if (_sigManifestCache != null && DateTime.UtcNow - _sigManifestCacheAt < TimeSpan.FromSeconds(60))
                return _sigManifestCache;

            var sb = new System.Text.StringBuilder();
            var root = @"C:\Users\LOSTE GAME\Desktop\rex room\RecRoom_Data";
            var placeholderSig = Convert.ToBase64String(new byte[256]); // 344 chars, RSA-2048 shape

            List<string> allDirs = new List<string> { root };
            try
            {
                foreach (var d in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
                    allDirs.Add(d);
            }
            catch { }

            foreach (var d in allDirs)
            {
                IEnumerable<string> files;
                try { files = Directory.GetFiles(d); } catch { continue; }
                foreach (var f in files)
                {
                    try
                    {
                        var rel = Path.GetRelativePath(root, f);
                        var fileName = Path.GetFileName(rel);
                        if (rel.StartsWith("BepInEx") || rel.StartsWith("EasyAntiCheat") ||
                            rel.StartsWith("logs") || rel.StartsWith("dotnet") ||
                            rel.Contains(Path.DirectorySeparatorChar + "Logs" + Path.DirectorySeparatorChar) ||
                            rel.Contains(Path.DirectorySeparatorChar + "Logs") ||
                            fileName.StartsWith(".") ||
                            fileName.Equals("verification.sig", StringComparison.OrdinalIgnoreCase) ||
                            rel.StartsWith(".."))
                            continue;
                        sb.Append(rel).Append('|').Append(placeholderSig).Append('\n');
                    }
                    catch { }
                }
            }

            _sigManifestCache = sb.ToString();
            _sigManifestCacheAt = DateTime.UtcNow;
            return _sigManifestCache;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[sigs] manifest build error: {ex.GetType().Name}: {ex.Message}");
            return "";
        }
    }
}