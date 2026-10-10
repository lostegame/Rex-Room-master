namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;

// GET /api/images/v5/player/{accountId}
//
// Reference returns a bare array of SavedImageDto (empty for most accounts).
// Backed by SavedImageStore so a photo taken in-game shows up on the profile.
public class ImagesV5PlayerApi : IApi
{
    public string Response(HttpContext context)
    {
        int accountId = 0;
        var segs = (context.Request.Path.Value ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segs.Length - 1; i++)
            if (string.Equals(segs[i], "player", StringComparison.OrdinalIgnoreCase))
                int.TryParse(segs[i + 1], out accountId);

        var sb = new StringBuilder("[");
        var items = SavedImageStore.ByPlayer(accountId);
        for (int i = 0; i < items.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(SavedImageStore.SavedImageDto(items[i]));
        }
        sb.Append(']');
        return sb.ToString();
    }

    public string GetUrl() => "/api/images/v5/player/{accountId}";
    public string GetMethod() => HttpMethods.Get;
}
