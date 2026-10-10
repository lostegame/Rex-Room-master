namespace Rex_Room_20230414.API;

public class Account
{
    public int Id { get; set; }
    public string PlatformId { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string DisplayName { get; set; }
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public bool IsNew { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public int Tokens { get; set; }
    public int Gold { get; set; }
    public string ProfileImage { get; set; }
    public string Bio { get; set; }
    public string LastLoginTime { get; set; }
    public string CreatedAt { get; set; }
    public bool IsJunior { get; set; }
    public int RegistrationStatus { get; set; }
    public int PersonalPronouns { get; set; }
    public int IdentityFlags { get; set; }
    public string DisplayEmoji { get; set; }
    public string Birthday { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string OutfitSelections { get; set; }
    public string OutfitSelectionsV2 { get; set; }
    public string FaceFeatures { get; set; }
    public string HairColor { get; set; }
    public string SkinColor { get; set; }
    public bool HasSetAvatar { get; set; }

    // Catalog item ids the account has permanently unlocked (level-up gifts,
    // purchases). The wardrobe (/econ/customAvatarItems/v1/owned) reports these
    // as IsOwned:true on top of the free starter set, so an item stays owned
    // across restarts instead of only living in the in-memory gift queue.
    public List<int> UnlockedAvatarItemIds { get; set; } = new List<int>();

    // Invention ids this account has actually BOUGHT. This is the single source of
    // truth for the "OWNED" state: /api/inventions/v2/mine lists these, and every
    // invention DTO reports OwnershipPurchased from here. Before this existed the
    // only signal was CreatorPlayerId, which is a different thing entirely - it
    // made the client render inventions it had never purchased as OWNED.
    public List<int> OwnedInventionIds { get; set; } = new List<int>();

    // LoginLock of the session that already collected the join reward, so the
    // 100-token + gift drop is granted once per entry rather than on every
    // heartbeat tick.
    public string LastRewardLoginLock { get; set; }
    public string LastRewardAt { get; set; }

    // Unopened gift boxes, in mint order - the store behind GET /api/avatar/v2/gifts.
    // Persisted here (not in an in-memory queue) so a box survives a restart and
    // so a box bought for one player can never surface in another player's list.
    // The shape is the reference GiftPackagePayload; see GiftPackage.cs.
    public List<GiftPackage> GiftPackages { get; set; } = new List<GiftPackage>();
}
