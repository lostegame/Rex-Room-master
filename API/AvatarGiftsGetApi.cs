namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /api/avatar/v2/gifts
// This used to answer a hardcoded "[]", which the reference capture also shows -
// but only because the account that was watched owned no boxes. The route itself
// has to stay (without it the /api/avatar/v2 prefix handler serves the outfit
// object and the client toasts "Failed to download gifts: An error occurred"),
// and now it serves this player's real unopened boxes.
//
// Reference, CLAUDE.md client-contract-notes: the stored box in
// GET /api/avatar/v2/gifts carries the same fields the GiftPackageReceived frames
// carry, and its AvatarItemType is NULL for anything that is not an avatar item.
// Answering [] unconditionally is what made a bought item and a received gift both
// look like they never arrived.
public class AvatarGiftsGetApi : IApi
{
    public string Response(HttpContext context)
    {
        int? accountId = AccountHelper.GetAccountIdFromToken(context);
        if (!accountId.HasValue || accountId.Value <= 0) return "[]";
        return GiftPackageStore.ListJson(accountId.Value);
    }
    public string GetUrl() => "/api/avatar/v2/gifts";
    public string GetMethod() => HttpMethods.Get;
}