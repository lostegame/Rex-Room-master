namespace Rex_Room_20230414.API;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

// GET /rooms/requiring/developer and /rooms/requiring/rrplus - the "needs dev approval"
// and "RR+" gated room lists the client requests on the home tab. Reference returns []
// for both; the gate flags come from each room entry, so we filter on them.
public class RoomsRequiringApi : IApi
{
    public string Response(HttpContext context)
    {
        string path = context.Request.Path.Value ?? "";
        var rooms = RoomCatalog.Hot();

        var filtered = path.Contains("developer", StringComparison.OrdinalIgnoreCase)
            ? rooms.Where(r => r.TryGetProperty("RequiresDeveloperApproval", out var v) &&
                               v.ValueKind == JsonValueKind.True)
            : rooms.Where(r => r.TryGetProperty("RequiresRrPlus", out var v) &&
                               v.ValueKind == JsonValueKind.True);

        return RoomCatalog.ListJson(filtered.ToArray(), 0);
    }

    public string GetUrl() => "/rooms/requiring/{requirement}";
    public string GetMethod() => HttpMethods.Get;
}
