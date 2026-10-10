namespace Rex_Room_20230414.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

// Saved images ("sharecamera" photos the player takes) are one resource behind
// four endpoints, so they live in a single store:
//
//   POST /api/images/v4/uploadsaved      -> {"ImageName":"sharecamera/<date>/<guid>.jpg"}
//   GET  /api/images/v6?name=<ImageName> -> SavedImageDto for that name
//   GET  /api/images/v4/room/{id}?sort=&filter=&take=&skip=  -> [RoomImageDto]
//   GET  /api/images/v5/cheered/bulk?id= -> [{"SavedImageId":N,"IsCheered":bool}]
//   GET  /api/images/v5/player/{id}      -> [SavedImageDto]
//
// Reference shapes (rex room LogOutput.log, 2023 build):
//   SavedImageDto  {"SavedImageId":N,"ImageName":"...","PlayerId":N,"RoomId":N,
//                   "PlayerEventId":0,"ClubId":0,"Description":"","Accessibility":1,
//                   "AccessibilityLocked":false,"SavedImageType":1,"CreatedAt":"<iso>",
//                   "CheerCount":0,"CommentCount":0}
//   RoomImageDto   {"Id":N,"Type":1,"Accessibility":1,"AccessibilityLocked":false,
//                   "ImageName":"...","Description":"","PlayerId":N,
//                   "TaggedPlayerIds":[N],"RoomId":N,"PlayerEventId":null,
//                   "CreatedAt":"<iso>","CheerCount":0,"CommentCount":0}
//
// uploadsaved previously answered SavedImageId 0, so every follow-up lookup by id
// found nothing and the room photo strip stayed empty.
public static class SavedImageStore
{
    public sealed class Entry
    {
        public int SavedImageId;
        public string ImageName = "";
        public int PlayerId;
        public int RoomId;
        public string CreatedAt = "";
        public int CheerCount;
        public int CommentCount;
    }

    private static readonly object _io = new();
    private static readonly List<Entry> _items = new();
    private static int _nextId = 71000; // reference ids live in the 70k-109k range

    private static string FilePath =>
        Path.Combine(AppContext.BaseDirectory, "Storage", "saved-images.json");

    static SavedImageStore()
    {
        try
        {
            if (!File.Exists(FilePath)) return;
            using var doc = JsonDocument.Parse(File.ReadAllBytes(FilePath));
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return;
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                _items.Add(new Entry
                {
                    SavedImageId = Int(e, "SavedImageId", _nextId++),
                    ImageName = Str(e, "ImageName", ""),
                    PlayerId = Int(e, "PlayerId", 0),
                    RoomId = Int(e, "RoomId", 0),
                    CreatedAt = Str(e, "CreatedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z"),
                    CheerCount = Int(e, "CheerCount", 0),
                    CommentCount = Int(e, "CommentCount", 0),
                });
            }
            foreach (var i in _items)
                if (i.SavedImageId >= _nextId) _nextId = i.SavedImageId + 1;
        }
        catch { }
    }

    private static void Save()
    {
        try
        {
            lock (_io)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var sb = new System.Text.StringBuilder();
                sb.Append('[');
                for (int i = 0; i < _items.Count; i++)
                {
                    var it = _items[i];
                    if (i > 0) sb.Append(',');
                    sb.Append("{\"SavedImageId\":").Append(it.SavedImageId)
                      .Append(",\"ImageName\":").Append(JsonSerializer.Serialize(it.ImageName))
                      .Append(",\"PlayerId\":").Append(it.PlayerId)
                      .Append(",\"RoomId\":").Append(it.RoomId)
                      .Append(",\"CreatedAt\":").Append(JsonSerializer.Serialize(it.CreatedAt))
                      .Append(",\"CheerCount\":").Append(it.CheerCount)
                      .Append(",\"CommentCount\":").Append(it.CommentCount).Append('}');
                }
                sb.Append(']');
                File.WriteAllText(FilePath, sb.ToString());
            }
        }
        catch { }
    }

    public static Entry Add(string imageName, int playerId, int roomId, byte[] bytes = null)
    {
        var e = new Entry
        {
            SavedImageId = _nextId++,
            ImageName = imageName,
            PlayerId = playerId,
            RoomId = roomId,
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z",
        };
        lock (_io) { _items.Add(e); Save(); }
        if (bytes != null && bytes.Length > 0) WriteBytes(e.SavedImageId, bytes);
        Console.WriteLine($"[SavedImageStore] #{e.SavedImageId} {imageName} player={playerId} room={roomId} bytes={bytes?.Length ?? 0}");
        return e;
    }

    // ---- image bytes -------------------------------------------------------
    //
    // The store used to persist ONLY the name. The client then asked
    // /img/sharecamera/<date>/<guid>.jpg for the photo and there was nothing
    // behind that path, so every saved picture fell through to the shared
    // default image. Uploadsaved even wrote the bytes to Player\{id}\profile.png
    // instead, which replaced the player's actual avatar with a room snapshot
    // (account 3's "profile" was a 2560x1440 room photo).
    //
    // The bytes are now stored per SavedImageId under Storage/saved-images/ and
    // the avatar is left alone.

    private static string BytesDir =>
        Path.Combine(AppContext.BaseDirectory, "Storage", "saved-images");

    private static string BytesPathFor(int savedImageId) =>
        Path.Combine(BytesDir, savedImageId + ".bin");

    public static void WriteBytes(int savedImageId, byte[] bytes)
    {
        try
        {
            lock (_io)
            {
                Directory.CreateDirectory(BytesDir);
                File.WriteAllBytes(BytesPathFor(savedImageId), bytes);
            }
        }
        catch { }
    }

    public static byte[] ReadBytes(int savedImageId)
    {
        try
        {
            var p = BytesPathFor(savedImageId);
            return File.Exists(p) ? File.ReadAllBytes(p) : null;
        }
        catch { return null; }
    }

    public static byte[] ReadBytesByName(string imageName)
    {
        var e = ByName(imageName);
        return e == null ? null : ReadBytes(e.SavedImageId);
    }

    public static Entry ByName(string imageName)
    {
        lock (_io)
        {
            foreach (var e in _items)
                if (string.Equals(e.ImageName, imageName, StringComparison.OrdinalIgnoreCase))
                    return e;
        }
        return null;
    }

    public static List<Entry> ByRoom(int roomId)
    {
        lock (_io)
        {
            var list = new List<Entry>();
            foreach (var e in _items)
                if (e.RoomId == roomId) list.Add(e);
            return list;
        }
    }

    public static List<Entry> ByPlayer(int playerId)
    {
        lock (_io)
        {
            var list = new List<Entry>();
            foreach (var e in _items)
                if (e.PlayerId == playerId) list.Add(e);
            return list;
        }
    }

    public static string SavedImageDto(Entry e) =>
        "{\"SavedImageId\":" + e.SavedImageId +
        ",\"ImageName\":\"" + RoomCatalogJsonEsc(e.ImageName) + "\"" +
        ",\"PlayerId\":" + e.PlayerId +
        ",\"RoomId\":" + e.RoomId +
        ",\"PlayerEventId\":0,\"ClubId\":0,\"Description\":\"\"" +
        ",\"Accessibility\":1,\"AccessibilityLocked\":false,\"SavedImageType\":1" +
        ",\"CreatedAt\":\"" + e.CreatedAt + "\"" +
        ",\"CheerCount\":" + e.CheerCount +
        ",\"CommentCount\":" + e.CommentCount + "}";

    // Room image feed uses Id/Type/TaggedPlayerIds instead of SavedImageId.
    public static string RoomImageDto(Entry e) =>
        "{\"Id\":" + e.SavedImageId +
        ",\"Type\":1,\"Accessibility\":1,\"AccessibilityLocked\":false" +
        ",\"ImageName\":\"" + RoomCatalogJsonEsc(e.ImageName) + "\"" +
        ",\"Description\":\"\",\"PlayerId\":" + e.PlayerId +
        ",\"TaggedPlayerIds\":[" + e.PlayerId + "]" +
        ",\"RoomId\":" + e.RoomId +
        ",\"PlayerEventId\":null,\"CreatedAt\":\"" + e.CreatedAt + "\"" +
        ",\"CheerCount\":" + e.CheerCount +
        ",\"CommentCount\":" + e.CommentCount + "}";

    private static string RoomCatalogJsonEsc(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static int Int(JsonElement e, string n, int fb) =>
        e.TryGetProperty(n, out var v) && v.TryGetInt32(out var i) ? i : fb;

    private static string Str(JsonElement e, string n, string fb) =>
        e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? fb : fb;
}
