namespace Rex_Room_20230414;

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;

// Builds our OWN reference log from live client traffic.
//
// The recflare/reference captures only cover the rooms + match apps. Anything in
// the Rec Center (inventions, storefront, progression, gamerewards) is missing
// from them, which is exactly why so many of those routes had to be guessed from
// sibling shapes. That guessing is what produced inventions that could not be
// bought and a tile that read OWNED for something unpurchased.
//
// So: record every request the client makes together with the FULL response body
// and which handler answered it. Over a few play sessions this file becomes a
// real, queryable reference for this server - and it also makes mismatches
// obvious, because a route that is answered by "(none - generic fallback)" is
// flagged in the summary at the bottom of every write.
//
// Output: <base>/Storage/reference-traffic.jsonl   (one JSON object per line)
//
// Set REXROOM_NO_RECORD=1 to disable.

public static class TrafficRecorder
{
    private static readonly ConcurrentDictionary<string, int> _byPath = new();
    private static readonly object _io = new();

    public static bool Enabled =>
        !string.Equals(Environment.GetEnvironmentVariable("REXROOM_NO_RECORD"), "1", StringComparison.OrdinalIgnoreCase);

    private static string FilePath =>
        Path.Combine(AppContext.BaseDirectory, "Storage", "reference-traffic.jsonl");

    private static string SummaryPath =>
        Path.Combine(AppContext.BaseDirectory, "Storage", "reference-summary.txt");

    public static void Record(int index, string method, string path, int status, string body, string handler)
    {
        if (!Enabled) return;

        string bare = path ?? "";
        int q = bare.IndexOf('?');
        if (q >= 0) bare = bare.Substring(0, q);

        bool fallback = handler.StartsWith("(none", StringComparison.Ordinal);
        string key = method + " " + bare;
        int n = _byPath.AddOrUpdate(key, 1, (_, v) => v + 1);

        string line =
            "{\"i\":" + index +
            ",\"t\":\"" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture) + "\"" +
            ",\"m\":\"" + Esc(method) + "\"" +
            ",\"p\":\"" + Esc(path ?? "") + "\"" +
            ",\"s\":" + status +
            ",\"n\":" + n +
            ",\"h\":\"" + Esc(handler ?? "") + "\"" +
            ",\"fallback\":" + (fallback ? "true" : "false") +
            ",\"len\":" + (body?.Length ?? 0) +
            ",\"body\":" + Quote(body ?? "") + "}";

        try
        {
            lock (_io)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.AppendAllText(FilePath, line + Environment.NewLine);
            }
        }
        catch { }

        if (fallback || index % 100 == 0) WriteSummary();
    }

    private static void WriteSummary()
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            int total = 0;
            foreach (var kv in _byPath) total += kv.Value;

            sb.AppendLine("=== path inventory (method + path, no query) ===");
            sb.AppendLine($"distinct routes : {_byPath.Count}");
            sb.AppendLine($"total requests  : {total}");
            sb.AppendLine($"routes NEVER matched a handler (all fallback): {_fallbackPaths.Count}");
            sb.AppendLine();

            var fallbacks = new System.Collections.Generic.List<string>();
            foreach (var kv in _byPath)
            {
                var e = kv.Key.Split(new[] { ' ' }, 2);
                string m = e.Length > 0 ? e[0] : "";
                string p = e.Length > 1 ? e[1] : "";
                bool isFallback = _fallbackPaths.ContainsKey(p);
                if (isFallback) fallbacks.Add($"{m} {p}   x{kv.Value}");
            }

            if (fallbacks.Count > 0)
            {
                sb.AppendLine("--- STILL UNHANDLED (fix these first) ---");
                fallbacks.Sort();
                fallbacks.ForEach(x => sb.AppendLine("  " + x));
                sb.AppendLine();
            }

            sb.AppendLine("--- all routes, sorted ---");
            var all = new System.Collections.Generic.List<string>();
            foreach (var kv in _byPath)
            {
                var e = kv.Key.Split(new[] { ' ' }, 2);
                all.Add($"x{kv.Value,-5} {e[0],-6} {(e.Length > 1 ? e[1] : "")}");
            }
            all.Sort();
            all.ForEach(x => sb.AppendLine("  " + x));

            lock (_io) File.WriteAllText(SummaryPath, sb.ToString());
        }
        catch { }
    }

    // Paths that are known to have fallen through at least once.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _fallbackPaths = new();

    public static void NoteFallback(string path)
    {
        int q = path.IndexOf('?');
        if (q >= 0) path = path.Substring(0, q);
        _fallbackPaths.TryAdd(path, true);
        WriteSummary();
    }

    private static string Esc(string s) =>
        s.Replace("\\", "\\\\")
         .Replace("\"", "\\\"")
         .Replace("\r", "\\r")
         .Replace("\n", "\\n")
         .Replace("\t", "\\t");

    // Always emit the body as a JSON string. Writing the body through verbatim
    // (as an optimisation for greppability) breaks the one-object-per-line
    // contract as soon as a handler pretty-prints its JSON across several
    // lines, which every reader of this file then fails to parse.
    private static string Quote(string body) => "\"" + Esc(body ?? "") + "\"";
}
