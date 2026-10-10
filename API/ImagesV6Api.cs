namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /api/images/v6?name=sharecamera%2F2026-10-01%2F<guid>.jpg
//
// Resolves a saved-image name (the exact string POST /api/images/v4/uploadsaved
// returned) back into its SavedImageDto. The reference 404s an unknown name:
//   {"success":false,"error":{"message":"not found"}}
public class ImagesV6Api : IApi
{
    private int _status = 200;

    public string Response(HttpContext context)
    {
        _status = 200;
        var name = context.Request.Query["name"].ToString();
        if (!string.IsNullOrEmpty(name)) name = Uri.UnescapeDataString(name);

        var entry = string.IsNullOrEmpty(name) ? null : SavedImageStore.ByName(name);
        if (entry == null)
        {
            _status = 404;
            return "{\"success\":false,\"error\":{\"message\":\"not found\"}}";
        }
        return SavedImageStore.SavedImageDto(entry);
    }

    public string GetUrl() => "/api/images/v6";
    public string GetMethod() => HttpMethods.Get;
    public int GetStatusCode() => _status;
}
