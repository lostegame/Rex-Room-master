namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

// GET /config/LoadingScreenTipData — reference #72, returns an ARRAY of loading-screen tips.
// Every request returns a freshly shuffled list of English tips paired with images from the
// "loadingscreen" folder. The gallery/rotation start is randomized and tracked so the first
// image/tip shown differs from the previous request as much as possible.
public class LoadingScreenTipDataApi : IApi
{
    private static readonly Random _rng = new();
    private static int _lastTipStart = -1;
    private static int _lastImageOffset = -1;

    private static readonly string[] Tips =
    [
        "Welcome back to your old home. The memories never died, and now we're here to write new ones together.",
        "The journey continues. The official servers may be gone, but the community keeps the game alive.",
        "It feels like we never left. Thank you for being part of this revival.",
        "Relive the glory days. Your favorite place is back.",
        "The old rules still apply. Remember the classic tactics you learned in the original game.",
        "Built from the ground up by the community. Help us improve the experience by reporting anything unusual.",
        "Explore the changes. We've fixed some of the old issues and are working toward a smoother experience.",
        "Protect this community. Let's keep the server friendly and free from toxic behavior.",
        "Help us keep going. This server is powered by the community.",
        "Invite your old friends. Tell your old clan members that the game is back!",
        "Stay connected on Discord and follow the latest server updates.",
        "Rec Room servers will return, bringing cross-play between different devices.",
        "Future versions of these servers will become open-source and publicly available.",
        "Rec Room will eventually return to the 2026 version.",
        "The server may experience some issues that could cause unexpected service interruptions. We apologize for any inconvenience.",
        "You can visit the Rec Center and meet new friends!",
        "You can enjoy races, and we may also introduce a Creator system for building custom rooms.",
        "We will hold an annual event to commemorate the shutdown of the official Rec Room servers.",
        "Battle Royale and Run From Bonny game modes will be available!"
    ];

    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg" };

    private static string ResolveLoadingScreenDir()
    {
        var candidates = new[]
        {
            System.IO.Path.Combine(AppContext.BaseDirectory, "loadingscreen"),
            System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "loadingscreen"))
        };
        foreach (var c in candidates)
            if (System.IO.Directory.Exists(c)) return c;
        return candidates[0];
    }

    public string Response(HttpContext context)
    {
        string lsDir = ResolveLoadingScreenDir();
        string[] images;
        if (System.IO.Directory.Exists(lsDir))
        {
            images = System.IO.Directory.GetFiles(lsDir)
                .Where(f => ImageExtensions.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant()))
                .Select(System.IO.Path.GetFileName)
                .Where(n => n != null)
                .Cast<string>()
                .ToArray();
        }
        else
        {
            images = Array.Empty<string>();
        }

        if (images.Length == 0)
            return FallbackResponse();

        int tipCount = Tips.Length;
        int tipStart;
        if (tipCount <= 1)
        {
            tipStart = 0;
        }
        else
        {
            do { tipStart = _rng.Next(tipCount); } while (tipStart == _lastTipStart);
            _lastTipStart = tipStart;
        }

        int imgOffset;
        if (images.Length <= 1)
        {
            imgOffset = 0;
        }
        else
        {
            do { imgOffset = _rng.Next(images.Length); } while (imgOffset == _lastImageOffset);
            _lastImageOffset = imgOffset;
        }

        var sb = new System.Text.StringBuilder();
        sb.Append('[');
        for (int i = 0; i < tipCount; i++)
        {
            int tipIdx = (tipStart + i) % tipCount;
            int imgIdx = (imgOffset + i) % images.Length;
            if (i > 0) sb.Append(',');
            sb.Append('{')
              .Append("\"Name\":\"").Append(System.Guid.NewGuid().ToString("N")).Append("\",")
              .Append("\"Title\":\"").Append(JsonEscape(Tips[tipIdx])).Append("\",")
              .Append("\"Message\":\"\",")
              .Append("\"RoomNames\":[],")
              .Append("\"Context\":0,")
              .Append("\"InputType\":0,")
              .Append("\"Visibility\":1,")
              .Append("\"AllowCycling\":true,")
              .Append("\"RestrictToNewUsers\":false,")
              .Append("\"ImageName\":\"").Append(JsonEscape(images[imgIdx])).Append("\",")
              .Append("\"PlatformMask\":175,")
              .Append("\"CreatedAt\":\"2019-02-28T18:27:25Z\"")
              .Append('}');
        }
        sb.Append(']');
        return sb.ToString();
    }

    private static string FallbackResponse() =>
        @"[{
            ""Name"":""bf5d8d41a7184e779399ff45a9e8785a"",
            ""Title"":""Welcome back to your old home. The memories never died, and now we're here to write new ones together."",
            ""Message"":"""",
            ""RoomNames"":[],
            ""Context"":0,
            ""InputType"":0,
            ""Visibility"":1,
            ""AllowCycling"":true,
            ""RestrictToNewUsers"":false,
            ""ImageName"":""tip.jpg"",
            ""PlatformMask"":175,
            ""CreatedAt"":""2019-02-28T18:27:25Z""
        }]";

    private static string JsonEscape(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", string.Empty).Replace("\n", string.Empty);

    public string GetUrl() => "/config/LoadingScreenTipData";
    public string GetMethod() => HttpMethods.Get;
}