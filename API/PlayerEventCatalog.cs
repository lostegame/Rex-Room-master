namespace Rex_Room_20230414.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

// Player events back the Watch / Events tab:
//
//   GET /api/playerevents/v1/search?skip=&take=&...  -> [PlayerEventDto]
//   GET /api/playerevents/v1/searchlive?skip=&take=    -> [PlayerEventDto] (live only)
//
// Reference DTO (rex room LogOutput.log, 2023 build), field for field:
// {"PlayerEventId":120,"CreatorPlayerId":4550,"ImageName":"sharecamera/...","RoomId":8091,
//  "SubRoomId":null,"ClubId":null,"Name":"...","Description":"...","StartTime":"<iso>",
//  "EndTime":"<iso>","AttendeeCount":32,"State":0,"Accessibility":1,"IsMultiInstance":false,
//  "SupportMultiInstanceRoomChat":false,"DefaultBroadcastPermissions":0,
//  "CanRequestBroadcastPermissions":0}
//
// Events are seeded from the room catalog rather than invented: each one points at
// a real seeded room so clicking through from Watch actually loads, which is the
// whole point of the tab. State 0 = scheduled, 1 = live.
public static class PlayerEventCatalog
{
    public sealed class Event
    {
        public int PlayerEventId;
        public int CreatorPlayerId;
        public string ImageName = "";
        public int RoomId;
        public int? SubRoomId;
        public int? ClubId;
        public string Name = "";
        public string Description = "";
        public DateTime StartTime;
        public DateTime EndTime;
        public int AttendeeCount;
        public int State;
        public int Accessibility = 1;
        public bool IsMultiInstance;
    }

    private static List<Event>? _events;

    private static List<Event> Events
    {
        get
        {
            if (_events != null) return _events;
            _events = Build();
            return _events;
        }
    }

    private static List<Event> Build()
    {
        var list = new List<Event>();

        // Event templates over real catalog rooms: (roomName, subRoomName, title, blurb, hoursFromNow, durationHours)
        var seed = new (string Room, string? SubRoom, string Title, string Blurb, int StartInHours, int LengthHours)[]
        {
            ("Dodgeball",        null,        "Dodgeball Night",              "Open dodgeball lobby. Teams of four, first to 30 wins.", 2, 3),
            ("Paintball",        "River",     "Paintball Rush",               "Fast rounds on River. Bring your own loadout.",        5, 2),
            ("PerformanceHall",  null,        "Live Performance",             "Community showcase on the Performance Hall stage.",   8, 4),
            ("Crescendo",        null,        "Crescendo Jam",                "Band rehearsal. Everyone welcome.",                  26, 3),
            ("Lounge",           null,        "Lounge Hangout",               "Just hanging out. Bring a friend.",                  30, 6),
            ("LaserTag",         null,        "Laser Tag Practice",           "Laser tag, private match.",                          50, 2),
            ("Soccer",           null,        "Soccer Scrimmage",              "Pick-up soccer. All skill levels.",                  74, 3),
            ("MakerRoom",        null,        "Maker Room Workshop",          "Build-along session. Bring a circuit idea.",         98, 5),
            ("TheRiseofJumbotron", null,      "Jumbotron Watch Party",        "Watch the game together on the big screen.",          26, 3),
            ("Park",             null,        "Park Photo Walk",              "Group photo walk around the Park.",                 122, 2),
            ("CyberJunkCity",    null,        "Cyber Junk Exploration",       "First-timer friendly run through Cyber Junk City.",  146, 3),
            ("BowlingAlley",     null,        "Bowling Night",                "All lanes open.",                                    170, 3),
        };

        int id = 100;
        int creator = 1;
        var now = DateTime.UtcNow;

        foreach (var s in seed)
        {
            if (!RoomCatalog.TryGetRoomByName(s.Room, out var roomEl)) continue;
            int roomId = 0;
            if (roomEl.TryGetProperty("RoomId", out var rid) && rid.TryGetInt32(out int rr)) roomId = rr;

            int? subRoomId = null;
            if (s.SubRoom != null &&
                roomEl.TryGetProperty("SubRooms", out var subs) && subs.ValueKind == JsonValueKind.Array)
            {
                foreach (var sub in subs.EnumerateArray())
                    if (sub.TryGetProperty("Name", out var nm) &&
                        string.Equals(nm.GetString(), s.SubRoom, StringComparison.OrdinalIgnoreCase) &&
                        sub.TryGetProperty("SubRoomId", out var sid) && sid.TryGetInt32(out int sri))
                    { subRoomId = sri; break; }
            }

            string image = "";
            if (roomEl.TryGetProperty("ImageName", out var img) && img.ValueKind == JsonValueKind.String)
                image = img.GetString() ?? "";

            var start = now.AddHours(s.StartInHours);
            list.Add(new Event
            {
                PlayerEventId = id++,
                CreatorPlayerId = creator,
                ImageName = image,
                RoomId = roomId,
                SubRoomId = subRoomId,
                ClubId = null,
                Name = s.Title,
                Description = s.Blurb,
                StartTime = start,
                EndTime = start.AddHours(s.LengthHours),
                AttendeeCount = 0,
                State = 0,
                Accessibility = 1,
                IsMultiInstance = false,
            });
        }

        // One always-live event so searchlive is never empty.
        if (RoomCatalog.TryGetRoomByName("RecCenter", out var rc))
        {
            int rcId = 0;
            if (rc.TryGetProperty("RoomId", out var v) && v.TryGetInt32(out int x)) rcId = x;
            list.Add(new Event
            {
                PlayerEventId = id++,
                CreatorPlayerId = 1,
                ImageName = "RecCenter.jpg",
                RoomId = rcId,
                Name = "Rec Center Hangout",
                Description = "Drop in and say hi. Always open.",
                StartTime = now.AddHours(-1),
                EndTime = now.AddHours(11),
                AttendeeCount = 0,
                State = 1,
                Accessibility = 1,
            });
        }

        return list;
    }

    public static List<Event> Search(string query, bool liveOnly, int skip, int take)
    {
        IEnumerable<Event> q = Events;
        if (liveOnly)
            q = q.Where(e => e.State == 1);
        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(e =>
                e.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
        }
        return q.Skip(Math.Max(0, skip)).Take(Math.Clamp(take, 1, 100)).ToList();
    }

    // Single-event lookup for /api/playerevents/v1/{eventId}. Returns null for an
    // unknown id so the caller can answer 404 the way the reference does.
    public static Event Find(int eventId) =>
        Events.FirstOrDefault(e => e.PlayerEventId == eventId);

    public static string Dto(Event e)
    {
        string sub = e.SubRoomId.HasValue ? e.SubRoomId.Value.ToString() : "null";
        string club = e.ClubId.HasValue ? e.ClubId.Value.ToString() : "null";
        string image = string.IsNullOrEmpty(e.ImageName) ? "null" : "\"" + Esc(e.ImageName) + "\"";
        return "{\"PlayerEventId\":" + e.PlayerEventId +
               ",\"CreatorPlayerId\":" + e.CreatorPlayerId +
               ",\"ImageName\":" + image +
               ",\"RoomId\":" + e.RoomId +
               ",\"SubRoomId\":" + sub +
               ",\"ClubId\":" + club +
               ",\"Name\":\"" + Esc(e.Name) + "\"" +
               ",\"Description\":\"" + Esc(e.Description) + "\"" +
               ",\"StartTime\":\"" + e.StartTime.ToString("yyyy-MM-ddTHH:mm:ssZ") + "\"" +
               ",\"EndTime\":\"" + e.EndTime.ToString("yyyy-MM-ddTHH:mm:ssZ") + "\"" +
               ",\"AttendeeCount\":" + e.AttendeeCount +
               ",\"State\":" + e.State +
               ",\"Accessibility\":" + e.Accessibility +
               ",\"IsMultiInstance\":" + (e.IsMultiInstance ? "true" : "false") +
               ",\"SupportMultiInstanceRoomChat\":false,\"DefaultBroadcastPermissions\":0" +
               ",\"CanRequestBroadcastPermissions\":0}";
    }

    private static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
}
