namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System.Text;

public class AvatarItemsApi : IApi
{
    public string Response(HttpContext context)
    {
        // Touch AvatarFreeItems so its static ctor appends the five free
        // starter items the generated catalog is missing. Without this the
        // catalog endpoint could be served before those rows are installed.
        _ = AvatarFreeItems.ExtrasInstalled;

        var sb = new StringBuilder();
        sb.Append('[');
        bool first = true;
        foreach (var item in AvatarCatalogData.All)
        {
            if (!first) sb.Append(',');
            first = false;
            // avatarItemId is 0 for every catalog row, exactly like the real
            // server. The catalog is NOT the ownership list: a real id here
            // collides with the ids used by defaultunlocked/owned and the client
            // then treats the whole catalog as already owned and renders nothing.
            // Ownership is decided by matching avatarItemDesc against those two
            // endpoints, never by this field.
            sb.Append("{\"avatarItemId\":0")
              .Append(",\"avatarItemDesc\":\"")
              .Append(item.Guid)
              .Append(",,,\",\"friendlyName\":\"")
              .Append(JsonEscape(item.Name))
              .Append("\",\"tooltip\":\"\",\"tagList\":\"\",\"avatarItemType\":0,\"rarity\":0,\"isBaseAvatarItem\":false}");
        }
        sb.Append(']');
        return sb.ToString();
    }

    private static string JsonEscape(string s) =>
        (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

    public string GetUrl() => "/api/avatar/v4/items";
    public string GetMethod() => HttpMethods.Get;
}