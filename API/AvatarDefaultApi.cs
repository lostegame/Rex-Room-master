namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System.Text;

// GET /api/avatar/v1/defaultunlocked
//
// Returns ONLY the free starter wardrobe, exactly like the reference capture
// (55 items: the hair set, two glasses, two beards, the plain shirts, skirts,
// headbands and caps). This endpoint used to emit the ENTIRE 864-item catalog,
// which marked every item in the game as already owned for free - the wardrobe
// showed no prices and nothing looked locked or purchasable.
//
// The rest of the catalog is reachable through /api/avatar/v4/items (catalog) and
// becomes owned via /econ/customAvatarItems/v1/owned.
public class AvatarDefaultApi : IApi
{
    public string Response(HttpContext context)
    {
        _ = AvatarFreeItems.ExtrasInstalled;

        var sb = new StringBuilder();
        sb.Append('[');
        bool first = true;

        // Emit the reference entries verbatim, packed desc and FriendlyName
        // included. The combo and variant slots are what make a coloured variant
        // of a free base item a distinct wardrobe row, so they must be preserved
        // rather than collapsed to the base guid.
        foreach (var (desc, name) in AvatarFreeItems.Entries)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append("{\"AvatarItemType\":0,\"AvatarItemDesc\":\"").Append(JsonEscape(desc));
            sb.Append("\",\"PlatformMask\":-1,\"FriendlyName\":\"").Append(JsonEscape(name));
            sb.Append("\",\"Tooltip\":\"\",\"Rarity\":0}");
        }

        sb.Append(']');
        return sb.ToString();
    }

    private static string JsonEscape(string s) =>
        (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

    public string GetUrl() => "/api/avatar/v1/defaultunlocked";
    public string GetMethod() => HttpMethods.Get;
}
