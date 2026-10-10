namespace Rex_Room_20230414.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

// GET /econ/customAvatarItems/v1/owned?skip=0&take=100
//
// "My wardrobe" - and Results contains ONLY the items the account actually
// owns, never the whole catalog. The reference returns
// {"Results":[],"TotalResults":0} for an account that owns nothing beyond the
// free starter set, and TotalResults is the count of OWNED items, not of the
// catalog.
//
// Getting this wrong is what caused both wardrobe bugs:
//   * returning the full catalog made every item look already unlocked and free
//     (the "everything is open" report), and
//   * paginating the catalog instead of the owned set made TotalResults=868 with
//     only 100 entries, so the client kept asking for more pages and the
//     download never completed ("Failed to download unlocked items").
//
// Ownership = the free starter set (AvatarFreeItems, which the client also reads
// from /api/avatar/v1/defaultunlocked) plus whatever is persisted on the account
// in UnlockedAvatarItemIds (level-up gifts, purchases, the worn-outfit grant).
public class CustomAvatarItemsOwnedApi : IApi
{
    public string Response(HttpContext context)
    {
        _ = AvatarFreeItems.ExtrasInstalled;

        var accountId = AccountHelper.GetAccountIdFromToken(context);
        var acc = accountId.HasValue ? AccountHelper.GetAccountById(accountId.Value) : null;

        var all = AvatarCatalogData.All;
        var freeSet = new HashSet<int>(AvatarFreeItems.CatalogIndices);

        // Only genuinely unlocked items. The free starter set is deliberately
        // EXCLUDED: the client already gets those from
        // /api/avatar/v1/defaultunlocked, and the reference returns
        // {"Results":[],"TotalResults":0} for a fresh account. Its wardrobe is
        // the union of the two lists.
        var owned = new HashSet<int>();
        foreach (int idx in GiftDrop.OwnedGiftCatalogIndices(acc))
            if (!freeSet.Contains(idx)) owned.Add(idx);

        // The client pages this list, so honour skip/take over the OWNED set and
        // report that count - not the catalog size - as TotalResults.
        var ownedIndices = owned.Where(i => i >= 0 && i < all.Count).OrderBy(i => i).ToList();

        int skip = QueryInt(context, "skip", 0);
        int take = QueryInt(context, "take", 0);
        if (skip < 0) skip = 0;
        if (take < 0) take = 0;

        var page = ownedIndices.Skip(skip);
        if (take > 0) page = page.Take(take);

        var results = page
            .Select(i => Item(i + 1, all[i].Name, all[i].Guid))
            .ToList();

        return $"{{\"Results\":[{string.Join(",", results)}],\"TotalResults\":{ownedIndices.Count}}}";
    }

    // One-off migration: an outfit saved while the server wrongly reported the
    // whole catalog as owned can contain non-free pieces. The client blocks
    // leaving the dorm until every worn item is in defaultunlocked + owned, so
    // grant the currently worn non-free pieces to the account. This is a
    // deliberate, persisted unlock - not a permanent "everything is owned".
    public static int GrantWornItems(Account acc)
    {
        if (acc == null) return 0;
        if (acc.UnlockedAvatarItemIds == null) acc.UnlockedAvatarItemIds = new List<int>();

        var freeSet = new HashSet<int>(AvatarFreeItems.CatalogIndices);
        int added = 0;
        foreach (string guid in WornGuids(acc))
        {
            int idx = IndexOf(guid);
            if (idx < 0 || freeSet.Contains(idx)) continue;
            int id = idx + 1;
            if (!acc.UnlockedAvatarItemIds.Contains(id))
            {
                acc.UnlockedAvatarItemIds.Add(id);
                added++;
            }
        }
        if (added > 0) AccountHelper.SaveAccount(acc);
        return added;
    }

    private static int IndexOf(string guid)
    {
        var all = AvatarCatalogData.All;
        for (int i = 0; i < all.Count; i++)
            if (string.Equals(all[i].Guid, guid, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    private static IEnumerable<string> WornGuids(Account acc)
    {
        if (acc == null) yield break;

        var guids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // OutfitSelections is the packed v1 form: "guid,combo,variant,,bodyPart;..."
        if (!string.IsNullOrEmpty(acc.OutfitSelections))
        {
            foreach (var part in acc.OutfitSelections.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (var slot in part.Split(','))
                {
                    var g = slot.Trim();
                    if (g.Length == 36) guids.Add(g);
                }
            }
        }

        // OutfitSelectionsV2 is the JSON form with PrefabGuid / CombinationGuid.
        if (!string.IsNullOrEmpty(acc.OutfitSelectionsV2))
        {
            try
            {
                using var doc = JsonDocument.Parse(acc.OutfitSelectionsV2);
                if (doc.RootElement.TryGetProperty("selections", out var sel) &&
                    sel.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in sel.EnumerateArray())
                    {
                        if (s.TryGetProperty("PrefabGuid", out var pg) && pg.ValueKind == JsonValueKind.String)
                        {
                            var g = pg.GetString();
                            if (!string.IsNullOrEmpty(g)) guids.Add(g);
                        }
                        if (s.TryGetProperty("CombinationGuid", out var cg) && cg.ValueKind == JsonValueKind.String)
                        {
                            foreach (var g in (cg.GetString() ?? "").Split(','))
                            {
                                var t = g.Trim();
                                if (t.Length == 36) guids.Add(t);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        foreach (var g in guids) yield return g;
    }

    private static int QueryInt(HttpContext context, string key, int fallback)
    {
        if (context.Request.Query.TryGetValue(key, out var v) &&
            int.TryParse(v.ToString(), out int parsed))
            return parsed;
        return fallback;
    }

    // Everything in this list is owned, so there is no price to advertise here -
    // token prices belong to the locked-items endpoint the client uses to render
    // the shop. priceTokens/salePercent/saleStartDate/saleEndDate are kept for
    // schema compatibility with the client's DTO.
    //
    // CustomAvatarItemId is an empty GUID, never null: the client parses this
    // field as a Guid, so a JSON null throws "ArgumentException: Invalid Guid
    // Pattern." during DownloadOwnedCustomAvatarItemsRecursive and the whole
    // owned-list fetch fails ("Unable to get Custom Avatar Items owned by the
    // for local player"). First-party items have no custom-avatar id, and the
    // reference server never returns this field at all, so Guid.Empty is the
    // only value that is both absent-in-reference and parseable-by-client.
    private static string Item(int id, string name, string guid)
    {
        var sb = new StringBuilder();
        sb.Append("{\"AvatarItemId\":").Append(id);
        sb.Append(",\"PrefabName\":\"").Append(Esc(name)).Append('"');
        sb.Append(",\"PrefabGuid\":\"").Append(Esc(guid)).Append('"');
        sb.Append(",\"AvatarItemDesc\":\"").Append(Esc(guid)).Append(",,,\"");
        sb.Append(",\"CustomAvatarItemId\":\"00000000-0000-0000-0000-000000000000\"");
        sb.Append(",\"ItemSetFriendlyName\":\"\"");
        sb.Append(",\"Description\":\"").Append(Esc(name)).Append('"');
        sb.Append(",\"Rarity\":0");
        sb.Append(",\"IsOwned\":true");
        sb.Append(",\"IsFavorited\":false");
        sb.Append(",\"IsBaseAvatarItem\":true");
        sb.Append(",\"priceTokens\":0");
        sb.Append(",\"salePercent\":0");
        sb.Append(",\"saleStartDate\":null");
        sb.Append(",\"saleEndDate\":null");
        sb.Append('}');
        return sb.ToString();
    }

    private static string Esc(string s) => (s ?? "")
        .Replace("\\", "\\\\").Replace("\"", "\\\"");

    public string GetUrl() => "/econ/customAvatarItems/v1/owned";
    public string GetMethod() => HttpMethods.Get;
}

// One-off migration for outfits saved while the server wrongly reported the
// whole catalog as owned. Without this the client refuses to let the player
// leave the dorm with "You're wearing items you don't own", because the
// equipped pieces are neither in defaultunlocked nor in UnlockedAvatarItemIds.
public class CustomAvatarItemsGrantWornApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context);
        if (!accountId.HasValue) return "{\"success\":false,\"granted\":0}";
        var acc = AccountHelper.GetAccountById(accountId.Value);
        int granted = CustomAvatarItemsOwnedApi.GrantWornItems(acc);
        return $"{{\"success\":true,\"accountId\":{accountId.Value},\"granted\":{granted}}}";
    }

    public string GetUrl() => "/econ/customAvatarItems/v1/grantworn";
    public string GetMethod() => HttpMethods.Get;
}
