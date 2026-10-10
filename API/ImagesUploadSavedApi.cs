namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using Rex_Room_20230414.Database;
using System;
using System.IO;
using System.Text;

public class ImagesUploadSavedApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context);

        // Buffer the request body ONCE, up front, and rewind it so anything that
        // reads it later (RoomData.SessionKey reads Request.Form for LoginLock)
        // still sees the full stream. Reading it ad-hoc further down left the body
        // already consumed, so SessionKey threw "Unexpected end of Stream" and the
        // upload 500'd - 5 times in the live log.
        try
        {
            if (context.Request.Body != null)
            {
                context.Request.EnableBuffering();
                context.Request.Body.Position = 0;
            }
        }
        catch { }

        byte[] rawBody = null;
        Console.WriteLine(
            $"[ImagesUpSld] ENTER method={context.Request.Method} " +
            $"contentType='{context.Request.ContentType}' " +
            $"contentLength={context.Request.ContentLength} " +
            $"bodyCanSeek={context.Request.Body.CanSeek} " +
            $"bodyCanRead={context.Request.Body.CanRead}");
        if (context.Request.Body != null)
        {
            // Read the buffered stream, then rewind again so later readers still work.
            using var ms = new MemoryStream();
            try
            {
                context.Request.Body.CopyTo(ms);
                if (context.Request.Body.CanSeek) context.Request.Body.Position = 0;
            }
            catch
            {
                // Fallback for streams that don't support CopyTo
                try
                {
                    if (context.Request.Body.CanSeek) context.Request.Body.Position = 0;
                    using var sr = new StreamReader(context.Request.Body, leaveOpen: true);
                    var txt = sr.ReadToEnd();
                    rawBody = Encoding.UTF8.GetBytes(txt);
                }
                catch { rawBody = null; }
            }
            if (rawBody == null)
                rawBody = ms.ToArray();

            Console.WriteLine(
                $"[ImagesUpSld] read {rawBody.Length} body bytes; " +
                $"head={(rawBody.Length >= 32 ? BitConverter.ToString(rawBody, 0, 32) : BitConverter.ToString(rawBody))}");
        }

        string contentType = context.Request.ContentType ?? "";
        bool multipart = contentType.Contains("multipart", StringComparison.OrdinalIgnoreCase);
        string boundary = "";
        int bIdx = contentType.IndexOf("boundary=", StringComparison.OrdinalIgnoreCase);
        if (multipart && bIdx >= 0)
        {
            // The Content-Type boundary ALREADY carries its leading dashes
            // (e.g. "------------------------xxxx"). The old code prepended another
            // "--" so every boundary lookup missed and no bytes were ever found.
            boundary = contentType.Substring(bIdx + "boundary=".Length).Trim().Trim('"');
        }

        byte[] imageBytes = null;
        if (rawBody != null && rawBody.Length > 0)
        {
            if (multipart && !string.IsNullOrEmpty(boundary))
                imageBytes = ExtractFirstFilePart(rawBody, boundary);
            else if (LooksLikeImage(rawBody))
                // Not multipart: the client posted the image bytes as the whole
                // body (Content-Type: image/jpeg / application/octet-stream).
                imageBytes = rawBody;
        }

        // A share-camera photo, not a profile picture: the reference names it under
        // sharecamera/ and the client then asks for it back via /api/images/v6?name=
        // and /api/images/v4/room/{id}. Registering it in SavedImageStore is what
        // makes those lookups resolve instead of 404ing.
        string imageName = $"sharecamera/{DateTime.UtcNow:yyyy-MM-dd}/{Guid.NewGuid():N}.jpg";
        int playerId = accountId ?? 0;
        int roomId = RoomData.AssignedRoomIdFor(context);

        if (imageBytes != null && imageBytes.Length > 0)
        {
            // Store the bytes with the saved-image entry, keyed by SavedImageId.
            //
            // It used to call SaveProfileImage here, which overwrote the player's
            // real avatar with a room snapshot - account 3's profile picture was
            // a 2560x1440 room photo as a result. A share-camera photo is not a
            // profile picture, and now that the bytes are stored the photo is
            // actually retrievable from /img/sharecamera/... instead of resolving
            // to the shared default image.
            var entry = SavedImageStore.Add(imageName, playerId, roomId, imageBytes);
            Console.WriteLine($"[ImagesUpSld] account={playerId} room={roomId} saved {imageBytes.Length} bytes as {imageName} (id={entry.SavedImageId})");
        }
        else
        {
            Console.WriteLine($"[ImagesUpSld] no image bytes captured (account={accountId}, multipart={multipart}, boundary='{boundary}', rawLen={rawBody?.Length ?? 0}); registering name-only {imageName}");
            SavedImageStore.Add(imageName, playerId, roomId);
        }

        // The reference body is exactly {"ImageName":"..."} - extra keys are not
        // needed and the client only reads ImageName off this response.
        return "{ \"ImageName\": \"" + imageName + "\" }";
    }

    // JPEG / PNG / GIF / WEBP magic bytes.
    private static bool LooksLikeImage(byte[] b)
    {
        if (b == null || b.Length < 4) return false;
        if (b[0] == 0xFF && b[1] == 0xD8) return true;                                  // JPEG
        if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return true;  // PNG
        if (b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46) return true;                  // GIF
        if (b.Length >= 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46
            && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return true; // WEBP
        return false;
    }

    // Walk the multipart parts byte-wise and return the payload of the first
    // part that carries a filename (or, failing that, the first part with a
    // non-empty body). Works on raw bytes so binary images survive intact.
    private static byte[] ExtractFirstFilePart(byte[] body, string boundary)
    {
        if (body == null || body.Length == 0 || string.IsNullOrEmpty(boundary)) return null;

        byte[] dashBoundary = Encoding.ASCII.GetBytes("--" + boundary);
        byte[] crlfcrlf = { 13, 10, 13, 10 };

        int pos = IndexOfBytes(body, dashBoundary, 0);
        while (pos >= 0)
        {
            int partStart = pos + dashBoundary.Length;
            if (partStart + 1 < body.Length && body[partStart] == 13 && body[partStart + 1] == 10)
                partStart += 2;

            int headerEnd = IndexOfBytes(body, crlfcrlf, partStart);
            if (headerEnd < 0) return null;

            int nextBoundary = IndexOfBytes(body, dashBoundary, headerEnd + 4);
            int dataEnd = nextBoundary < 0 ? body.Length : nextBoundary;

            // Drop the CRLF that precedes the boundary.
            if (dataEnd - 2 >= headerEnd + 4 && body[dataEnd - 2] == 13 && body[dataEnd - 1] == 10)
                dataEnd -= 2;

            int len = dataEnd - (headerEnd + 4);
            if (len > 0)
            {
                bool hasFilename = IndexOfBytes(body, Encoding.ASCII.GetBytes("filename="), partStart) >= 0
                                   && IndexOfBytes(body, Encoding.ASCII.GetBytes("filename="), partStart) < headerEnd;
                if (hasFilename || LooksLikeImage(Slice(body, headerEnd + 4, len)))
                {
                    return Slice(body, headerEnd + 4, len);
                }
            }

            if (nextBoundary < 0) return null;
            pos = nextBoundary;
        }

        return null;
    }

    private static byte[] Slice(byte[] src, int offset, int len)
    {
        var dst = new byte[len];
        Array.Copy(src, offset, dst, 0, len);
        return dst;
    }

    private static int IndexOfBytes(byte[] haystack, byte[] needle, int start = 0)
    {
        if (haystack == null || needle == null || needle.Length == 0) return -1;
        int limit = haystack.Length - needle.Length;
        for (int i = Math.Max(0, start); i <= limit; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j]) { match = false; break; }
            }
            if (match) return i;
        }
        return -1;
    }

    public string GetUrl() => "/api/images/v4/uploadsaved";
    public string GetMethod() => HttpMethods.Post;
}