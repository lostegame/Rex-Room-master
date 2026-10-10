namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// POST /api/avatar/v2/gifts/consume/
// Opening a box. The route existed but answered {"error":"","success":true,
// "value":null} for every id, so a box could never actually be opened and the
// wearable it named was never unlocked.
//
// Two reference rules shape the answer:
//   - "Endpoints the client re-renders from must return the updated entity, not
//      { error, success, value: null }" (CLAUDE.md client-contract-notes), so the
//      opened box is returned in `value` rather than null.
//   - avatarItemType on a box is NULL unless the box holds an avatar item; here
//      the box is being opened, so it is returned exactly as it was minted.
//
// The box is removed from the store BEFORE the ids are unlocked, so a retry of
// the same id cannot grant twice. Unlocking happens here, not at purchase time:
// the reference has the purchase write ownership and put the box in the bag, but
// a wearable only becomes wearable once its box is opened.
public class AvatarGiftsApi : IApi
{
    public string Response(HttpContext context)
    {
        int? accountId = AccountHelper.GetAccountIdFromToken(context);
        if (!accountId.HasValue || accountId.Value <= 0)
            return "{\"error\":\"NotAuthorized\",\"success\":false,\"value\":null}";

        int id = FormInt(context, "giftPackageId", "GiftPackageId");
        if (id <= 0) id = FormInt(context, "id", "packageId");
        if (id <= 0)
            return "{\"error\":\"\",\"success\":false,\"value\":null}";

        var box = GiftPackageStore.Consume(accountId.Value, id);
        if (box == null)
        {
            Console.WriteLine($"[gifts/consume] account={accountId.Value} id={id} NOT FOUND");
            return "{\"error\":\"\",\"success\":false,\"value\":null}";
        }

        var acc = AccountHelper.GetAccountById(accountId.Value);
        var unlocked = UnlockAvatarItems(acc, box);
        if (acc != null) AccountHelper.SaveAccount(acc);

        return "{\"error\":\"\",\"success\":true,\"value\":" + box.ToJson() + ",\"UnlockedAvatarItemIds\":[" +
               string.Join(",", unlocked) + "]}";
    }

    // A box naming avatar items unlocks them. Idempotent: an id already in
    // UnlockedAvatarItemIds is not reported again, so re-opening a consumed box
    // (which the store now refuses) and a double-clicked consume agree.
    private static List<int> UnlockAvatarItems(Account acc, GiftPackage box)
    {
        var granted = new List<int>();
        if (acc == null || box?.AvatarItemIds == null || box.AvatarItemIds.Count == 0) return granted;
        if (acc.UnlockedAvatarItemIds == null) acc.UnlockedAvatarItemIds = new List<int>();

        foreach (int id in box.AvatarItemIds)
        {
            if (id <= 0) continue;
            if (acc.UnlockedAvatarItemIds.Contains(id)) continue;
            acc.UnlockedAvatarItemIds.Add(id);
            granted.Add(id);
        }
        if (granted.Count > 0)
            Console.WriteLine($"[gifts/consume] account={acc.Id} unlocked {string.Join(",", granted)}");
        return granted;
    }

    private static int FormInt(HttpContext context, string key, string altKey)
    {
        string raw = AccountHelper.FormValue(context, key, altKey);
        if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw.Trim(), out int v)) return v;
        return 0;
    }

    public string GetUrl() => "/api/avatar/v2/gifts/consume/";
    public string GetMethod() => HttpMethods.Post;
}