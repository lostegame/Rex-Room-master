namespace Rex_Room_20230414.API;
using System;
using System.Collections.Generic;
using System.Linq;

// The FREE starter wardrobe.
//
// The reference capture (LogOutput_live.log, GET /api/avatar/v1/defaultunlocked)
// returns 55 entries, and the list is NOT "the whole catalog": it is the starter
// set a brand-new account owns before spending anything - the hair set, the
// starter glasses, the beards, the plain shirts, the tennis skirts, the
// headbands and the caps.
//
// Before this existed, defaultunlocked returned the ENTIRE 864-item catalog as
// free, so the wardrobe showed every item as already unlocked with no price and
// nothing looked purchasable. The two endpoints are deliberately different:
//
//   /api/avatar/v1/defaultunlocked     -> the 55 free entries (below)
//   /econ/customAvatarItems/v1/owned   -> what the account actually owns
//
// An AvatarItemDesc is a 4-slot packed string "baseGuid,comboGuid,variantGuid,"
// (the trailing slot is empty in every capture). Coloured variants of the same
// base item are separate entries, which is why the same base guid appears more
// than once below - 55 entries over 48 distinct base items.
public static class AvatarFreeItems
{
    // Verbatim (AvatarItemDesc, FriendlyName) pairs from the reference capture,
    // in capture order. Extracted from the log with a script rather than written
    // by hand: hand transcription corrupted guids (05f0ee6e-...-5ed5767ba04 vs
    // the real ...5ed5767c6fe0c, and 4aa6 vs 4ea6) and silently mis-aligned the
    // variant guids, which is what made the free wardrobe come out wrong.
    public static readonly (string Desc, string Name)[] Entries =
    {
        ("5d13a7a2-8213-40e6-90a6-efdd76a3fdcb,,,", "Flowing Hair"),
        ("1d27b674-f9e2-4ffc-9d8c-a58a1be06457,,,", "Afro Hair"),
        ("d84c0ff9-8fbe-4ed8-abf3-7996e81888ab,,,", "Large Afro Hair"),
        ("e5b83dfc-b2e1-4dcb-a4ab-9d3a4c8a34ae,,,", "Long Wavy Hair"),
        ("7dd6f7b0-7ba0-429f-a04f-e32d3a79ee61,,,", "Short Wavy Hair"),
        ("eb9611c6-bb50-41a2-93e9-7f959815a846,,,", "Dreads Long Hair"),
        ("1fd69ef8-0b74-4962-af5a-67f0bf0358f2,,,", "Ponytail Hair"),
        ("a12f724f-4a73-4ab8-aad4-6bfc662b4dd6,,,", "Undercut Long Hair"),
        ("0753d7a4-8247-4fca-a6fc-359c26086140,,,", "Fonzie Hair"),
        ("77d3c585-4928-4471-a425-89036efe7299,,,", "Spiky Hair"),
        ("92302d9d-c527-418c-ac5d-1fa869727505,,,", "Part Hair"),
        ("f9dd08f8-16d3-4c39-af4f-89f7bb6e80d3,,,", "Undercut Short Hair"),
        ("b148cb1e-df81-442f-aea6-ab1727aad00e,,,", "Chunky Afro Hair"),
        ("e36bcd98-7e85-43fa-89f8-57e4ec33823a,,,", "Bob with Bangs Hair"),
        ("880a3cc0-7407-4b61-b759-f9dd890fe9e5,,,", "Bob Hair"),
        ("21599b51-c50f-43d8-ac5f-62c30cd02ca5,,,", "Lori Hair"),
        ("193a3bf9-abc0-4d78-8d63-92046908b1c5,,,", "Emo Hair"),
        ("79b90274-6eec-4664-acfb-4a123334661e,,,", "Pig Tails Hair"),
        ("da4e7b34-2095-4a9e-801e-4f409039e0dd,,,", "Buzz Cut Hair"),
        ("9d9fadb6-97eb-480e-a224-4e0179082071,,,", "Meatball Buns Hair"),
        ("d8280c0c-d803-4513-be10-a0ba96d8821e,,,", "Flowhawk Hair"),
        ("e286863c-2967-4d00-b837-b49487b9484a,,,", "Fauxhawk Hair"),
        ("2cb4f372-3372-4583-8b57-c4e3988e3c28,,,", "Punky Hair"),
        ("06306723-ca20-4aa6-b7b3-917113f41ac3,,,", "Cat-Eye Glasses (Red)"),
        ("c70005d5-6276-4a98-acb3-6a77bc19379a,,,", "Glasses (Teal)"),
        ("8d10cc78-6b00-45f3-affb-205e9cc5b03f,,,", "Beard (Close)"),
        ("cc96f8a5-bc5b-4f89-83b7-ecd53905ada7,,,", "Beard (Thick)"),
        ("c6c08eb5-381a-4193-9722-80da95d62abe,,,", "Business Tie (Black)"),
        ("4d507dfa-4a99-4ac0-8537-229e9dc0eb4a,,,", "Rec Room Tank Top (Orange)"),
        ("d0a9262f-5504-46a7-bb10-7507503db58e,,,", "Rec Room Shirt (Crew Neck, White)"),
        ("d0a9262f-5504-46a7-bb10-7507503db58e,95e4cc30-cb68-473d-a395-feadf5b51512,0440f08f-ef1d-49d8-942b-523056e8bb45,", "Rec Room T-Shirt (Crew Neck, Orange)"),
        ("2e59d8d0-91a0-4449-bfdc-a5d663fd9343,,,", "Collared Shirt (Plaid, Blue)"),
        ("7b857a8c-92ad-4028-a2c2-b3c20cdab5f2,,,", "T-Shirt"),
        ("8aa79563-ace1-4ba7-ad0c-f3210a78142f,,,", "Rec Room Shirt (V-Neck, White)"),
        ("8aa79563-ace1-4ba7-ad0c-f3210a78142f,95e4cc30-cb68-473d-a395-feadf5b51512,05f0ee6e-c824-470e-9178-5ed576c6fe0c,", "Rec Room T-Shirt (V-Neck, Orange)"),
        ("21caa68e-c3fa-474c-af5e-af1e742b7a60,,,", "Tennis Skirt (Blue)"),
        ("21caa68e-c3fa-474c-af5e-af1e742b7a60,c5deba2a-6e35-4b13-8e94-8ba5457f39df,b75ef67d-00c3-4ac1-9b72-212032460294,", "Tennis Skirt (Yellow)"),
        ("21caa68e-c3fa-474c-af5e-af1e742b7a60,758752bd-db2f-43d2-b580-55b3e1efffd5,b75ef67d-00c3-4ac1-9b72-212032460294,", "Tennis Skirt (Red)"),
        ("2296ed0d-df56-4d46-b33a-aae9230a47fc,,,", "Zipper Dress (Yellow)"),
        ("ecc1dbe6-ca06-4564-b2a6-30956194d1e9,,,", "Wristbands (White)"),
        ("71921831-ba6f-408b-a00e-2fd97663636f,,,", "Wrist Tape (White)"),
        ("2e59d8d0-91a0-4449-bfdc-a5d663fd9343,55901f12-d5b5-4fa8-b4c8-e479689ee39d,f600037d-c9c0-43fa-b45b-02f456f9dd5f,", "Collared Shirt (Denim)"),
        ("2e59d8d0-91a0-4449-bfdc-a5d663fd9343,bf82f2f6-9af8-431e-a296-0890dea48ba7,d015cae7-a905-49e4-8823-6dec069689a6,", "Collared Shirt (Argyle)"),
        ("2e59d8d0-91a0-4449-bfdc-a5d663fd9343,EfdMcnfHt0mr0PQ_maaYOg,DRJcNhkqvkKFEaZpOguR6w,", "Collared Shirt (Flowers, Green)"),
        ("7b857a8c-92ad-4028-a2c2-b3c20cdab5f2,6d703981-2734-4c45-8983-cdd5f328902f,a0271cd0-e172-4d3f-aa2f-9806f21a82d2,", "Tank Top (Camo)"),
        ("7b857a8c-92ad-4028-a2c2-b3c20cdab5f2,5c4a2b35-0e1c-44de-8c3a-96d4a6458b1b,9c03f381-7357-4d0f-8cda-8737d4c43d25,", "Tank Top (Rainbow)"),
        ("7b857a8c-92ad-4028-a2c2-b3c20cdab5f2,51ef8d39-2b94-4f9e-9620-07b6b0a913a5,d2a692e6-e1a9-4cfe-8154-10b52be7f8c8,", "Jersey (Orange)"),
        ("7b857a8c-92ad-4028-a2c2-b3c20cdab5f2,ad61c418-6d77-4a99-8ac5-9f10f5a3d42f,b292eb4b-07e3-4a48-99b5-3c6587a1e02e,", "Tank Top (Dots)"),
        ("7b857a8c-92ad-4028-a2c2-b3c20cdab5f2,48abd952-214f-48b2-a8f1-1146f6f69aa2,b78008e8-abbd-4ece-be34-9a911f721fcc,", "Tank Top (Zebra)"),
        ("14ef6b00-debf-4a85-9755-b4d37df496d3,8377ab96-c908-457f-9fee-b784c9a759f3,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", "Baseball Cap (Red)"),
        ("14ef6b00-debf-4a85-9755-b4d37df496d3,dee70c38-7a99-4c2b-9181-665f1bf75aca,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", "Baseball Cap (Blue)"),
        ("40528de7-38a3-4a7c-8f93-6d3bfa5573f2,7d8e55fe-3c34-4b4b-9753-0021f6cc6454,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", "Headband (Cream)"),
        ("40528de7-38a3-4a7c-8f93-6d3bfa5573f2,1b1d08f2-12ca-43dd-a44f-ea2820b919b4,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", "Headband (Black)"),
        ("40528de7-38a3-4a7c-8f93-6d3bfa5573f2,51ef8d39-2b94-4f9e-9620-07b6b0a913a5,018a5c07-e956-457d-a540-a5e2cd68da09,", "Headband (Orange, White)"),
        ("40528de7-38a3-4a7c-8f93-6d3bfa5573f2,cbe29e9f-f2ac-47fb-97e1-8bad16abb89d,018a5c07-e956-457d-a540-a5e2cd68da09,", "Headband (Pink, White)"),
    };

    public static readonly string[] Descs = Entries.Select(e => e.Desc).ToArray();

    // Five free starter items the reference offers but the generated catalog is
    // missing entirely. Without these rows the wardrobe could not offer them at
    // all, so the client rendered five permanently absent pieces of the default
    // outfit. Guid -> the first FriendlyName the reference gives that base item.
    public static readonly (string Guid, string Name)[] ExtraFreeCatalog =
    {
        ("c6c08eb5-381a-4193-9722-80da95d62abe", "Business Tie (Black)"),
        ("4d507dfa-4a99-4ac0-8537-229e9dc0eb4a", "Rec Room Tank Top (Orange)"),
        ("d0a9262f-5504-46a7-bb10-7507503db58e", "Rec Room Shirt (Crew Neck, White)"),
        ("21caa68e-c3fa-474c-af5e-af1e742b7a60", "Tennis Skirt (Blue)"),
    };

    // Ensure the missing starter items exist in the catalog so they can be
    // listed, priced and equipped like any other item. Appended, not inserted,
    // so existing catalog indices (and any saved item ids) stay stable.
    public static readonly bool ExtrasInstalled = InstallExtras();

    private static bool InstallExtras()
    {
        var present = new HashSet<string>(
            AvatarCatalogData.All.Select(x => x.Guid), StringComparer.OrdinalIgnoreCase);
        int added = 0;
        foreach (var (guid, name) in ExtraFreeCatalog)
        {
            if (present.Add(guid))
            {
                AvatarCatalogData.All.Add((name, guid));
                added++;
            }
        }
        return added > 0;
    }

    private static readonly Dictionary<string, int> _descToCatalogIndex = BuildIndex();

    private static Dictionary<string, int> BuildIndex()
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var all = AvatarCatalogData.All;
        for (int i = 0; i < all.Count; i++)
        {
            if (!map.ContainsKey(all[i].Guid)) map[all[i].Guid] = i;
        }
        return map;
    }

    // Catalog indices (0-based) of the free starter wardrobe, ascending.
    public static readonly List<int> CatalogIndices = BuildFreeIndices();

    private static List<int> BuildFreeIndices()
    {
        var result = new List<int>();
        var seen = new HashSet<int>();
        foreach (var desc in Descs)
        {
            var baseGuid = desc.Split(',')[0];
            if (_descToCatalogIndex.TryGetValue(baseGuid, out int idx) && seen.Add(idx))
                result.Add(idx);
        }
        result.Sort();
        return result;
    }

    private static readonly HashSet<int> _freeSet = new HashSet<int>(CatalogIndices);

    public static bool IsFreeCatalogIndex(int index) => _freeSet.Contains(index);
}
