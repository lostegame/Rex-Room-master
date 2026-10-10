namespace Rex_Room_20230414.Database;

using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public static class DatabaseManager
{
    private static readonly string BaseDirectory =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Player"));
    private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // Every persisted file used to be written with File.WriteAllText straight onto
    // the target path, which truncates the file first and only then writes the new
    // content. A reader arriving inside that window parses a half-written file, the
    // catch blocks below swallow the parse error, and the request is answered from
    // an EMPTY account - so the player's wardrobe, backpack, friendships and
    // settings silently come back empty for that one request. That is what made
    // bought clothes "vanish" and the backpack look empty: those reads simply landed
    // inside an unrelated write, and the data was never actually lost.
    //
    // Write to a unique temp file in the SAME directory and then move it over the
    // target. The rename is atomic on the same volume, so a reader only ever sees
    // the complete old file or the complete new one.
    private static void WriteAtomic(string path, byte[] data)
    {
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string tmp = Path.Combine(
            string.IsNullOrEmpty(dir) ? "." : dir,
            Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");

        try
        {
            File.WriteAllBytes(tmp, data);
            File.Move(tmp, path, true);
        }
        catch
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            throw;
        }
    }

    // No BOM, matching what File.WriteAllText produced on .NET Core, so switching
    // the writes over does not change the on-disk encoding.
    private static void WriteAtomic(string path, string text) =>
        WriteAtomic(path, new System.Text.UTF8Encoding(false).GetBytes(text));

    // Readers still get one retry: an atomic write cannot be observed half-done, but
    // external tools (editors, scripts, backups) can still truncate a file, and a
    // single blip should not empty the account for that request.
    private static string ReadAllTextStable(string path)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try { return File.ReadAllText(path); }
            catch when (attempt < 2) { Thread.Sleep(15); }
        }
        return File.ReadAllText(path);
    }

    public static async Task SaveAsync(string identifier, string key, string data)
    {
        string userDirectory = Path.Combine(BaseDirectory, identifier);
        
        if (!Directory.Exists(userDirectory))
        {
            Directory.CreateDirectory(userDirectory);
        }

        string filePath = Path.Combine(userDirectory, $"{key}.json");
        
        WriteAtomic(filePath, data);
        await Task.CompletedTask;
    }

    public static async Task<string> LoadAsync(string identifier, string key)
    {
        string filePath = Path.Combine(BaseDirectory, identifier, $"{key}.json");

        if (File.Exists(filePath))
        {
            return await File.ReadAllTextAsync(filePath);
        }

        return string.Empty;
    }

    // ---- Account persistence -------------------------------------------------

    private static string AccountFilePath(int accountId) =>
        Path.Combine(BaseDirectory, accountId.ToString(), "account.json");

    private static string ProfileFilePath(int accountId) =>
        Path.Combine(BaseDirectory, accountId.ToString(), "profile.png");

    public static List<API.Account> LoadAllPlayerData()
    {
        var accounts = new List<API.Account>();
        if (!Directory.Exists(BaseDirectory))
            return accounts;

        foreach (var dir in Directory.GetDirectories(BaseDirectory))
        {
            var file = Path.Combine(dir, "account.json");
            if (!File.Exists(file)) continue;
            try
            {
                var json = ReadAllTextStable(file);
                var acc = JsonSerializer.Deserialize<API.Account>(json, _jsonOpts);
                if (acc != null) accounts.Add(acc);
            }
            catch { }
        }
        return accounts;
    }

    public static void SavePlayerData(API.Account account)
    {
        if (account == null) return;
        string dir = Path.Combine(BaseDirectory, account.Id.ToString());
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string json = JsonSerializer.Serialize(account, _jsonOpts);
        WriteAtomic(Path.Combine(dir, "account.json"), json);
    }

    private static string UsernamesFilePath() => Path.Combine(BaseDirectory, "__usernames.json");

    // ---- Player settings (key-value) persistence -----------------------------

    private static string SettingsFilePath(int accountId) =>
        Path.Combine(BaseDirectory, "Settings", accountId.ToString(), "settings.json");

    public static Dictionary<string, string> LoadSettings(int accountId)
    {
        var path = SettingsFilePath(accountId);
        if (File.Exists(path))
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(
                    ReadAllTextStable(path), _jsonOpts) ?? new Dictionary<string, string>();
            }
            catch { }
        }
        return new Dictionary<string, string>();
    }

    public static void SaveSetting(int accountId, string key, string value)
    {
        var data = LoadSettings(accountId);
        data[key] = value;
        string dir = Path.Combine(BaseDirectory, "Settings", accountId.ToString());
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        WriteAtomic(SettingsFilePath(accountId), JsonSerializer.Serialize(data, _jsonOpts));
    }

    public static List<string> LoadUsernames()
    {
        var path = UsernamesFilePath();
        if (!File.Exists(path))
        {
            // Derive from stored accounts if the index has never been built
            var names = new List<string>();
            foreach (var acc in LoadAllPlayerData())
                if (!string.IsNullOrEmpty(acc.Username)) names.Add(acc.Username);
            return names;
        }
        try
        {
            return JsonSerializer.Deserialize<List<string>>(ReadAllTextStable(path), _jsonOpts) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public static void RegisterUsername(string username)
    {
        if (string.IsNullOrEmpty(username)) return;
        var names = LoadUsernames();
        if (names.Contains(username, System.StringComparer.OrdinalIgnoreCase)) return;
        names.Add(username);
        string dir = Path.Combine(BaseDirectory);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        WriteAtomic(UsernamesFilePath(), JsonSerializer.Serialize(names, _jsonOpts));
    }

    public static void RemoveUsername(string username)
    {
        if (string.IsNullOrEmpty(username)) return;
        var names = LoadUsernames();
        names = names.Where(n => !string.Equals(n, username, System.StringComparison.OrdinalIgnoreCase)).ToList();
        string dir = Path.Combine(BaseDirectory);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        WriteAtomic(UsernamesFilePath(), JsonSerializer.Serialize(names, _jsonOpts));
    }

    public static byte[]? GetProfileImage(int accountId)
    {
        var path = ProfileFilePath(accountId);
        if (File.Exists(path))
            return File.ReadAllBytes(path);
        return null;
    }

    public static void SaveProfileImage(int accountId, byte[] data)
    {
        if (data == null) return;
        string dir = Path.Combine(BaseDirectory, accountId.ToString());
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        WriteAtomic(ProfileFilePath(accountId), data);
    }
}
