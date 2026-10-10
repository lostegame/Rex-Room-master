namespace Rex_Room_20230414.API;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

// Deterministic placeholder art, generated per image name.
//
// WHY THIS EXISTS
// The 2023 RecRoom client fetches every image from <Images>/img/<ImageName>.
// Room DTOs carry names like "RecCenter.jpg" and inventions carry
// "invention/<date>/<hash>.jpg". The original images lived on img.rec.net, which
// no longer resolves in DNS, and this project ships no room or invention art. So
// the /img handler fell through to one catch-all that served
// Content/default_profile.jpg - which is why EVERY tile in the game looked like
// the same default avatar.
//
// A single shared file is also the worst possible fallback: it hides which entity
// is broken and makes the whole UI look identical. Instead we synthesise a stable
// image per name - same name always yields the same bytes, different names look
// different, and the label on it tells you which entity the tile is.
//
// Format is PNG (written by hand: IHDR/IDAT/IEND + CRC32) because it needs no
// native codec, is lossless, and the client decodes by content rather than by
// file extension - so serving PNG bytes for a .jpg URL is fine.

public static class PlaceholderImageFactory
{
    private const int Size = 256;
    private const int Margin = 14;

    private static readonly Dictionary<string, byte[]> _cache = new();
    private static readonly object _lock = new();

    // Name -> PNG bytes. Deterministic for the life of the process.
    public static byte[] Get(string imageName)
    {
        string key = imageName ?? "";
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached)) return cached;
        }

        byte[] built;
        try { built = Build(key); }
        catch { return null; }

        lock (_lock)
        {
            // Bound the cache so a client asking for thousands of invented
            // invention paths cannot grow this without limit.
            if (_cache.Count > 512) _cache.Clear();
            _cache[key] = built;
        }
        return built;
    }

    private static byte[] Build(string imageName)
    {
        // Stable 32-bit hash of the name -> hue, so a given room always looks
        // the same colour.
        uint h = Fnv1a(imageName);

        byte r1 = (byte)(60 + h % 150);
        byte g1 = (byte)(60 + (h >> 8) % 150);
        byte b1 = (byte)(60 + (h >> 16) % 150);
        byte r2 = (byte)Math.Max(20, r1 / 2);
        byte g2 = (byte)Math.Max(20, g1 / 2);
        byte b2 = (byte)Math.Max(20, b1 / 2);

        var px = new byte[Size * Size * 3];

        // Diagonal gradient background.
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                int t = (x + y) / (Size * 2);
                int i = (y * Size + x) * 3;
                px[i] = (byte)(r2 + (r1 - r2) * t);
                px[i + 1] = (byte)(g2 + (g1 - g2) * t);
                px[i + 2] = (byte)(b2 + (b1 - b2) * t);
            }
        }

        // A couple of soft bands so it does not read as a flat swatch.
        int band = (int)(h % (uint)Size);
        for (int y = 0; y < Size; y++)
        {
            int d = Math.Abs(y - band);
            if (d < 10)
            {
                Lighten(px, y, Size, d);
            }
        }

        string label = LabelFor(imageName);
        DrawText(px, label, (byte)(r1 > 140 && g1 > 140 && b1 > 140 ? 0 : 255));

        return EncodePng(px, Size, Size);
    }

    private static void Lighten(byte[] px, int y, int width, int d)
    {
        int amount = 12 - d;
        if (amount <= 0) return;
        for (int x = 0; x < width; x++)
        {
            int i = (y * width + x) * 3;
            px[i] = (byte)Math.Min(255, px[i] + amount);
            px[i + 1] = (byte)Math.Min(255, px[i + 1] + amount);
            px[i + 2] = (byte)Math.Min(255, px[i + 2] + amount);
        }
    }

    // "invention/2026-10-01/abcdef01....jpg" -> "ABCDEF01", which is far more
    // useful than a 40-character hash when you are staring at a broken tile.
    private static string LabelFor(string imageName)
    {
        string s = imageName ?? "";
        if (s.Length == 0) return "IMAGE";

        int slash = s.LastIndexOf('/');
        if (slash >= 0) s = s.Substring(slash + 1);

        int dot = s.LastIndexOf('.');
        if (dot > 0) s = s.Substring(0, dot);

        s = s.ToUpperInvariant();

        // If it is mostly hex digits, show only the leading few so it fits.
        bool hexish = true;
        foreach (char c in s)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'))) { hexish = false; break; }
        }
        if (hexish && s.Length > 8) s = s.Substring(0, 8);

        if (s.Length == 0) return "IMAGE";
        if (s.Length > 10) s = s.Substring(0, 10);
        return s;
    }

    // ---- 5x7 bitmap font, enough for A-Z 0-9 and a few punctuation marks -----

    private static readonly Dictionary<char, byte[]> Font = new()
    {
        ['A'] = new byte[] { 0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
        ['B'] = new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x11, 0x11, 0x1E },
        ['C'] = new byte[] { 0x0E, 0x11, 0x10, 0x10, 0x10, 0x11, 0x0E },
        ['D'] = new byte[] { 0x1E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x1E },
        ['E'] = new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x1F },
        ['F'] = new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x10 },
        ['G'] = new byte[] { 0x0E, 0x11, 0x10, 0x17, 0x11, 0x11, 0x0F },
        ['H'] = new byte[] { 0x11, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
        ['I'] = new byte[] { 0x0E, 0x04, 0x04, 0x04, 0x04, 0x04, 0x0E },
        ['J'] = new byte[] { 0x07, 0x02, 0x02, 0x02, 0x02, 0x12, 0x0C },
        ['K'] = new byte[] { 0x11, 0x12, 0x14, 0x18, 0x14, 0x12, 0x11 },
        ['L'] = new byte[] { 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x1F },
        ['M'] = new byte[] { 0x11, 0x1B, 0x15, 0x15, 0x11, 0x11, 0x11 },
        ['N'] = new byte[] { 0x11, 0x19, 0x15, 0x13, 0x11, 0x11, 0x11 },
        ['O'] = new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
        ['P'] = new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10 },
        ['Q'] = new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x15, 0x12, 0x0D },
        ['R'] = new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x14, 0x12, 0x11 },
        ['S'] = new byte[] { 0x0F, 0x10, 0x10, 0x0E, 0x01, 0x01, 0x1E },
        ['T'] = new byte[] { 0x1F, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 },
        ['U'] = new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
        ['V'] = new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x0A, 0x04 },
        ['W'] = new byte[] { 0x11, 0x11, 0x11, 0x15, 0x15, 0x1B, 0x11 },
        ['X'] = new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x0A, 0x11, 0x11 },
        ['Y'] = new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x04, 0x04, 0x04 },
        ['Z'] = new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x10, 0x1F },
        ['0'] = new byte[] { 0x0E, 0x11, 0x13, 0x15, 0x19, 0x11, 0x0E },
        ['1'] = new byte[] { 0x04, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x0E },
        ['2'] = new byte[] { 0x0E, 0x11, 0x01, 0x06, 0x08, 0x10, 0x1F },
        ['3'] = new byte[] { 0x1F, 0x02, 0x04, 0x02, 0x01, 0x11, 0x0E },
        ['4'] = new byte[] { 0x02, 0x06, 0x0A, 0x12, 0x1F, 0x02, 0x02 },
        ['5'] = new byte[] { 0x1F, 0x10, 0x1E, 0x01, 0x01, 0x11, 0x0E },
        ['6'] = new byte[] { 0x06, 0x08, 0x10, 0x1E, 0x11, 0x11, 0x0E },
        ['7'] = new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x08, 0x08 },
        ['8'] = new byte[] { 0x0E, 0x11, 0x11, 0x0E, 0x11, 0x11, 0x0E },
        ['9'] = new byte[] { 0x0E, 0x11, 0x11, 0x0F, 0x01, 0x02, 0x0C },
        ['-'] = new byte[] { 0x00, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00 },
        ['_'] = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F },
        ['.'] = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x0C, 0x0C },
        [' '] = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
    };

    private static void DrawText(byte[] px, string text, byte colour)
    {
        const int glyphW = 5, glyphH = 7, gap = 1;
        int scale = 3;
        int charW = (glyphW + gap) * scale;
        int totalW = text.Length * charW - gap * scale;

        // Shrink until it fits inside the margins.
        while (scale > 1 && (totalW > Size - 2 * Margin || glyphH * scale > Size / 2)) scale--;
        totalW = text.Length * charW - gap * scale;

        int x0 = (Size - totalW) / 2;
        int y0 = (Size - glyphH * scale) / 2;

        // Dark backing plate so light text stays readable on light backgrounds.
        for (int y = y0 - 4; y < y0 + glyphH * scale + 4; y++)
        {
            for (int x = x0 - 6; x < x0 + totalW + 6; x++)
            {
                if (x < 0 || y < 0 || x >= Size || y >= Size) continue;
                int i = (y * Size + x) * 3;
                px[i] = (byte)(px[i] / 3);
                px[i + 1] = (byte)(px[i + 1] / 3);
                px[i + 2] = (byte)(px[i + 2] / 3);
            }
        }

        for (int ci = 0; ci < text.Length; ci++)
        {
            if (!Font.TryGetValue(text[ci], out var glyph)) continue;
            for (int gy = 0; gy < glyphH; gy++)
            {
                for (int gx = 0; gx < glyphW; gx++)
                {
                    if ((glyph[gy] & (1 << (glyphW - 1 - gx))) == 0) continue;
                    for (int sy = 0; sy < scale; sy++)
                    {
                        for (int sx = 0; sx < scale; sx++)
                        {
                            int x = x0 + ci * charW + gx * scale + sx;
                            int y = y0 + gy * scale + sy;
                            if (x < 0 || y < 0 || x >= Size || y >= Size) continue;
                            int i = (y * Size + x) * 3;
                            px[i] = colour;
                            px[i + 1] = colour;
                            px[i + 2] = colour;
                        }
                    }
                }
            }
        }
    }

    private static uint Fnv1a(string s)
    {
        uint hash = 2166136261;
        foreach (char c in s ?? "")
        {
            hash ^= c;
            hash *= 16777619;
        }
        return hash;
    }

    // ---- minimal PNG writer (8-bit truecolour, filter type 0) ----------------

    private static readonly uint[] CrcTable = BuildCrcTable();

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }

    private static uint Crc32(byte[] data, int length)
    {
        uint c = 0xFFFFFFFFu;
        for (int i = 0; i < length; i++)
            c = CrcTable[(c ^ data[i]) & 0xFF] ^ (c >> 8);
        return c ^ 0xFFFFFFFFu;
    }

    private static byte[] EncodePng(byte[] rgb, int width, int height)
    {
        // Raw scanlines: 1 filter byte (0 = None) then RGB triplets.
        var raw = new byte[height * (1 + width * 3)];
        int o = 0;
        for (int y = 0; y < height; y++)
        {
            raw[o++] = 0;
            Buffer.BlockCopy(rgb, y * width * 3, raw, o, width * 3);
            o += width * 3;
        }

        byte[] compressed;
        using (var ms = new MemoryStream())
        {
            // ZLib wrapper: 0x78 0x01 then the raw deflate stream.
            using (var z = new ZLibStream(ms, CompressionLevel.Optimal, true))
                z.Write(raw, 0, raw.Length);
            compressed = ms.ToArray();
        }

        using var outMs = new MemoryStream();
        outMs.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0, 8);

        var ihdr = new byte[13];
        WriteBigEndian(ihdr, 0, width);
        WriteBigEndian(ihdr, 4, height);
        ihdr[8] = 8;    // bit depth
        ihdr[9] = 2;    // colour type: truecolour
        ihdr[10] = 0;   // compression
        ihdr[11] = 0;   // filter
        ihdr[12] = 0;   // interlace
        WriteChunk(outMs, "IHDR", ihdr);
        WriteChunk(outMs, "IDAT", compressed);
        WriteChunk(outMs, "IEND", Array.Empty<byte>());

        return outMs.ToArray();
    }

    private static void WriteChunk(Stream s, string type, byte[] data)
    {
        var len = new byte[4];
        WriteBigEndian(len, 0, data.Length);
        s.Write(len, 0, 4);

        var typeBytes = new byte[4];
        for (int i = 0; i < 4; i++) typeBytes[i] = (byte)type[i];
        s.Write(typeBytes, 0, 4);

        s.Write(data, 0, data.Length);

        var crcInput = new byte[4 + data.Length];
        Buffer.BlockCopy(typeBytes, 0, crcInput, 0, 4);
        if (data.Length > 0) Buffer.BlockCopy(data, 0, crcInput, 4, data.Length);
        uint crc = Crc32(crcInput, crcInput.Length);

        var crcBytes = new byte[4];
        WriteBigEndian(crcBytes, 0, (int)crc);
        s.Write(crcBytes, 0, 4);
    }

    private static void WriteBigEndian(byte[] buf, int offset, int value)
    {
        buf[offset] = (byte)((value >> 24) & 0xFF);
        buf[offset + 1] = (byte)((value >> 16) & 0xFF);
        buf[offset + 2] = (byte)((value >> 8) & 0xFF);
        buf[offset + 3] = (byte)(value & 0xFF);
    }
}
