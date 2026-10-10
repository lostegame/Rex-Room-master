namespace Rex_Room_20230414.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

// Inventions back the Rec Center invention browser:
//
//   GET /api/inventions/v2/search?skip=&take=            -> [InventionDto]
//   GET /api/inventions/v1/featured                     -> [InventionDto]
//   GET /api/inventions/v1/toptoday                      -> [InventionDto]
//   GET /api/inventions/v1/version?inventionId=&version= -> InventionVersionDto
//   GET /api/inventions/v1/fromcreators                  -> [creator summary]
//
// Reference InventionDto (rex room LogOutput.log):
// {"InventionId":26289,"ReplicationId":"<guid>","CreatorPlayerId":263,"Name":"...",
//  "Description":"...","ImageName":"invention/<date>/<guid>.jpg","CurrentVersionNumber":1,
//  "CurrentVersion":{ ...InventionVersionDto... }}
//
// Reference InventionVersionDto:
// {"InventionId":N,"ReplicationId":"<guid>","VersionNumber":1,"BlobName":"<date>/<guid>.inv",
//  "BlobHash":"<base64>","InstantiationCost":N,"LightsCost":0,"ChipsCost":0,
//  "CloudVariablesCost":0,"AICost":0,...}
//
// NOTE: the reference itself answers 500 on /api/inventions/v1/toptoday. We answer
// a well-formed list instead: same route, valid JSON, and the client can render it.
//
// These are generated locally with stable ids and costs - no third-party invention
// names, replication ids or blobs are reproduced here.
public static class InventionCatalog
{
    public sealed class Invention
    {
        public int InventionId;
        public string ReplicationId = "";
        public int CreatorPlayerId;
        public string Name = "";
        public string Description = "";
        public string ImageName = "";
        public int CurrentVersionNumber = 1;
        public int InstantiationCost = 100;
        public int LightsCost;
        public int ChipsCost;
        public int CloudVariablesCost;
        public int AICost;
    }

    private static List<Invention>? _items;

    private static List<Invention> Items
    {
        get
        {
            if (_items != null) return _items;
            _items = Build();
            return _items;
        }
    }

    private static List<Invention> Build()
    {
        var seed = new (string Name, string Desc, int Cost)[]
        {
            ("Ping Pong",          "Classic two-paddle rally court.",              120),
            ("Hoops",              "Shoot hoops with friends.",                     150),
            ("Laser Tag Arena",    "Fast-paced laser tag arena.",                   180),
            ("Obstacle Course",    "Timed obstacle course with checkpoints.",       200),
            ("Treasure Hunt",      "Follow the clues, find the chest.",             140),
            ("Music Stage",        "Stage with instruments and lights.",            260),
            ("Racing Track",       "Loop track for racing circuits.",                220),
            ("Hide and Seek",      "Hiders and seekers in a maze.",                 190),
            ("Tower Defense",      "Place defenses and stop the waves.",            240),
            ("Mini Golf",          "Nine holes of mini golf.",                      130),
            ("Dance Floor",        "Dance floor with mirror ball.",                 170),
            ("Paint Arena",        "Paint the walls and the floor.",               110),
        };

        var list = new List<Invention>();
        int id = 26090;
        foreach (var s in seed)
        {
            // Deterministic ids so a given index always maps to the same invention.
            list.Add(new Invention
            {
                InventionId = id++,
                ReplicationId = DeterministicGuid("inv" + s.Name),
                CreatorPlayerId = 1,
                Name = s.Name,
                Description = s.Desc,
                ImageName = "invention/" + DateTime.UtcNow.ToString("yyyy-MM-dd") + "/" +
                            DeterministicGuid(s.Name).Replace("-", "") + ".jpg",
                CurrentVersionNumber = 1,
                InstantiationCost = s.Cost,
                LightsCost = 0,
                ChipsCost = 0,
                CloudVariablesCost = 0,
                AICost = 0,
            });
        }
        return list;
    }

    // Stable guid from a string so ids/blobs do not change between boots.
    private static string DeterministicGuid(string seed)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(seed));
        return new Guid(bytes).ToString();
    }

    public static Invention Find(int inventionId) =>
        Items.FirstOrDefault(i => i.InventionId == inventionId);

    public static List<Invention> Page(int skip, int take) =>
        Items.Skip(Math.Max(0, skip)).Take(Math.Clamp(take, 1, 100)).ToList();

    // Search page used by /api/inventions/v2/search?value=<term>&skip=&take=
    //
    // The reference filters on the term and returns [] when nothing matches:
    //   ?value=%23inventiontower&skip=0&take=32  ->  []
    // A leading '#' is the reference's tag marker, so it is searched against the
    // name too - "#inventiontower" finds inventions named "inventiontower".
    // An empty/absent term returns the unfiltered page.
    public static List<Invention> Search(string value, int skip, int take)
    {
        take = Math.Clamp(take, 1, 100);
        skip = Math.Max(0, skip);
        var term = (value ?? "").Trim().TrimStart('#');
        if (term.Length == 0) return Page(skip, take);

        return Items.Where(i =>
                   i.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   i.Description.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Skip(skip).Take(take).ToList();
    }

    public static string VersionDto(Invention i)
    {
        string blobName = DateTime.UtcNow.ToString("yyyy-MM-dd") + "/" +
                          DeterministicGuid(i.Name + "v" + i.CurrentVersionNumber).Replace("-", "") + ".inv";
        // The reference BlobHash is base64 of a 32-byte digest.
        string blobHash;
        using (var sha = System.Security.Cryptography.SHA256.Create())
            blobHash = Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(blobName)));

        return "{\"InventionId\":" + i.InventionId +
               ",\"ReplicationId\":\"" + i.ReplicationId + "\"" +
               ",\"VersionNumber\":" + i.CurrentVersionNumber +
               ",\"BlobName\":\"" + blobName + "\"" +
               ",\"BlobHash\":\"" + blobHash + "\"" +
               ",\"InstantiationCost\":" + i.InstantiationCost +
               ",\"LightsCost\":" + i.LightsCost +
               ",\"ChipsCost\":" + i.ChipsCost +
               ",\"CloudVariablesCost\":" + i.CloudVariablesCost +
               ",\"AICost\":" + i.AICost + "}";
    }

    // Ownership flags are always emitted, driven by the real per-account
    // inventory, so the tile shows a price until the invention is genuinely
    // bought and only then reads OWNED. OwnershipPurchased is the field the
    // Rec Center client binds; IsOwned is mirrored for older paths. Omitting both
    // left the client guessing, which is how un-purchased inventions rendered as
    // OWNED.
    public static string Dto(Invention i, int viewerAccountId = 0)
    {
        bool owned = viewerAccountId > 0 && PlayerEconomy.OwnsInvention(viewerAccountId, i.InventionId);
        return "{\"InventionId\":" + i.InventionId +
        ",\"ReplicationId\":\"" + i.ReplicationId + "\"" +
        ",\"CreatorPlayerId\":" + i.CreatorPlayerId +
        ",\"Name\":\"" + Esc(i.Name) + "\"" +
        ",\"Description\":\"" + Esc(i.Description) + "\"" +
        ",\"ImageName\":\"" + Esc(i.ImageName) + "\"" +
        ",\"CurrentVersionNumber\":" + i.CurrentVersionNumber +
        ",\"OwnershipPurchased\":" + (owned ? "true" : "false") +
        ",\"IsOwned\":" + (owned ? "true" : "false") +
        ",\"IsFavorited\":false" +
        ",\"CurrentVersion\":" + VersionDto(i) + "}";
    }

    private static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
