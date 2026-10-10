namespace Rex_Room_20230414.API;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

// Room interactions: cheer (like) and favorite, per account, persisted.
//
// Reference (rooms.app.ts §24-27, 02-rooms-app.md:1463-1561):
//   PUT    /rooms/{roomId}/interactionby/me/cheer     -> toggle, return InteractionDto
//   DELETE /rooms/{roomId}/interactionby/me/cheer     -> remove cheer, return InteractionDto
//   PUT    /rooms/{roomId}/interactionby/me/favorite  -> toggle, return InteractionDto
//   DELETE /rooms/{roomId}/interactionby/me/favorite  -> remove favorite, return InteractionDto
//   GET    /rooms/{roomId}/interactionby/me           -> the same InteractionDto
//
// InteractionDto (05-rooms-dto.md:522) is PascalCase and exactly three keys:
//   { Cheered: bool, Favorited: bool, LastVisitedAt: string }
// LastVisitedAt is stamped with "now" on every read, never the stored value.
//
// The client registers these under the /roomserver prefix (discovery base URL is
// .../roomserver), so BOTH the prefixed and unprefixed paths are served.
public static class RoomInteractionStore
{
    private static readonly ConcurrentDictionary<string, (bool Cheered, bool Favorited)> _map = new();
    private static readonly object _io = new();

    private static string FilePath =>
        Path.Combine(AppContext.BaseDirectory, "Storage", "room-interactions.json");

    private static string Key(int accountId, int roomId) => accountId + ":" + roomId;

    static RoomInteractionStore()
    {
        try
        {
            if (File.Exists(VisitedFilePath))
            {
                var arr = JsonSerializer.Deserialize<string[]>(File.ReadAllBytes(VisitedFilePath));
                if (arr != null) foreach (var k in arr) _visited.Add(k);
            }
        }
        catch { }

        try
        {
            if (!File.Exists(FilePath)) return;
            using var doc = JsonDocument.Parse(File.ReadAllBytes(FilePath));
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return;
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                var parts = p.Name.Split(':');
                if (parts.Length != 2) continue;
                if (!int.TryParse(parts[0], out int acc) || !int.TryParse(parts[1], out int room)) continue;
                bool c = p.Value.TryGetProperty("Cheered", out var ce) && ce.GetBoolean();
                bool f = p.Value.TryGetProperty("Favorited", out var fe) && fe.GetBoolean();
                _map[p.Name] = (c, f);
            }
        }
        catch { /* first run: start empty */ }
    }

    private static void Save()
    {
        try
        {
            lock (_io)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var sb = new System.Text.StringBuilder();
                sb.Append('{');
                bool first = true;
                foreach (var kv in _map)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append(System.Text.Json.JsonSerializer.Serialize(kv.Key));
                    sb.Append(":{\"Cheered\":").Append(kv.Value.Cheered ? "true" : "false")
                      .Append(",\"Favorited\":").Append(kv.Value.Favorited ? "true" : "false").Append('}');
                }
                sb.Append('}');
                File.WriteAllText(FilePath, sb.ToString());
            }
        }
        catch { }
    }

    public static (bool Cheered, bool Favorited) Get(int accountId, int roomId)
        => _map.TryGetValue(Key(accountId, roomId), out var v) ? v : (false, false);

    // null = leave that flag untouched, so a cheer toggle never clears a favorite.
    public static void Set(int accountId, int roomId, bool? cheered, bool? favorited)
    {
        var cur = Get(accountId, roomId);
        _map[Key(accountId, roomId)] = (cheered ?? cur.Cheered, favorited ?? cur.Favorited);
        Save();
    }

    public static bool? ToggleCheer(int accountId, int roomId)
    {
        var cur = Get(accountId, roomId);
        var next = !cur.Cheered;
        _map[Key(accountId, roomId)] = (next, cur.Favorited);
        Save();
        return next;
    }

    public static bool? ToggleFavorite(int accountId, int roomId)
    {
        var cur = Get(accountId, roomId);
        var next = !cur.Favorited;
        _map[Key(accountId, roomId)] = (cur.Cheered, next);
        Save();
        return next;
    }

    public static List<int> RoomsCheeredBy(int accountId)
    {
        var list = new List<int>();
        foreach (var kv in _map)
        {
            var parts = kv.Key.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int acc) &&
                acc == accountId && kv.Value.Cheered && int.TryParse(parts[1], out int room))
                list.Add(room);
        }
        return list;
    }

    public static List<int> RoomsFavoritedBy(int accountId)
    {
        var list = new List<int>();
        foreach (var kv in _map)
        {
            var parts = kv.Key.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int acc) &&
                acc == accountId && kv.Value.Favorited && int.TryParse(parts[1], out int room))
                list.Add(room);
        }
        return list;
    }

    // Records that the account actually entered the room. Called from the
    // matchmake handler, which is the point where the client commits to joining,
    // so this is what /rooms/visitedby/me lists. Merely reading
    // /rooms/{id}/interactionby/me does NOT count as a visit - the client reads
    // that for rooms it is only browsing, and counting those would make the
    // "visited" tab show every room the player has ever scrolled past.
    public static void MarkVisited(int accountId, int roomId)
    {
        var key = Key(accountId, roomId);
        _visited.Add(key);
        SaveVisited();
    }

    private static readonly HashSet<string> _visited = new();

    private static string VisitedFilePath =>
        Path.Combine(AppContext.BaseDirectory, "Storage", "room-visits.json");

    private static void SaveVisited()
    {
        try
        {
            lock (_io)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(VisitedFilePath));
                var arr = _visited.ToArray();
                Array.Sort(arr, StringComparer.Ordinal);
                File.WriteAllText(VisitedFilePath,
                    JsonSerializer.Serialize(arr));
            }
        }
        catch { }
    }

    public static List<int> RoomsVisitedBy(int accountId)
    {
        var list = new List<int>();
        foreach (var key in _visited)
        {
            var parts = key.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int acc) &&
                acc == accountId && int.TryParse(parts[1], out int room))
                list.Add(room);
        }
        list.Sort();
        return list;
    }

    public static string Dto(int accountId, int roomId)
    {
        var s = Get(accountId, roomId);
        return "{\"Cheered\":" + (s.Cheered ? "true" : "false") +
               ",\"Favorited\":" + (s.Favorited ? "true" : "false") +
               ",\"LastVisitedAt\":\"" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z\"}";
    }
}

public class RoomInteractionByMeApi : IApi
{
    private readonly string _url;
    private readonly string _verb; // cheer | favorite | read

    public RoomInteractionByMeApi(string url = "/roomserver/rooms/{roomId}/interactionby/me",
                                  string verb = "read")
    {
        _url = url;
        _verb = verb;
    }

    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 1;
        int roomId = RoomInteractionByMeApi.RoomIdFromPath(context.Request.Path.Value ?? "");

        switch (_verb)
        {
            case "read":
                // A plain read must not count as entering the room, so the store is
                // only queried - MarkVisited is not called here.
                break;
            case "cheer-toggle":
                RoomInteractionStore.ToggleCheer(accountId, roomId);
                break;
            case "cheer-remove":
                RoomInteractionStore.Set(accountId, roomId, false, null);
                break;
            case "favorite-toggle":
                RoomInteractionStore.ToggleFavorite(accountId, roomId);
                break;
            case "favorite-remove":
                RoomInteractionStore.Set(accountId, roomId, null, false);
                break;
        }

        return RoomInteractionStore.Dto(accountId, roomId);
    }

    public static int RoomIdFromPath(string path)
    {
        var segs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segs.Length - 1; i++)
            if (string.Equals(segs[i], "rooms", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(segs[i + 1], out int id))
                return id;
        return 2;
    }

    public string GetUrl() => _url;
    public string GetMethod() => _verb.StartsWith("cheer") || _verb.StartsWith("favorite")
        ? (_verb.EndsWith("remove") ? HttpMethods.Delete : HttpMethods.Put)
        : HttpMethods.Get; // "read"
}
