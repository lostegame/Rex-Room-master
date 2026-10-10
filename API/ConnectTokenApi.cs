namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public class ConnectTokenApi : IApi
{
    private static readonly object _lock = new object();
    private static readonly byte[] _jwtSecret = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
    private static readonly string[] _tempDisplayNames = new[]
    {
        "BlazingHawk3729",
        "BlazingHawk6390",
        "BlazingMeteor4902",
        "BlazingNova4848",
        "BlazingPanther6736",
        "BlazingPanther9018",
        "BlazingShadow8347",
        "BoldBear4023",
        "BoldEagle1170",
        "BoldFalcon59",
        "BoldFalcon6071",
        "BoldFalcon894",
        "BoldHawk4119",
        "BoldJaguar4093",
        "BoldJaguar5978",
        "BoldLion5239",
        "BoldLynx1270",
        "BoldNinja1772",
        "BoldNinja4211",
        "BoldRobot9591",
        "BoldStorm9126",
        "BoldSun9660",
        "BoldWave6907",
        "BraveEagle401",
        "BraveEagle6127",
        "BraveKnight7934",
        "BraveMoon6292",
        "BraveMoon8929",
        "BravePixel7351",
        "BraveShark8838",
        "BraveSphinx5928",
        "BraveStar3913",
        "BraveWolf2497",
        "BreezyBear259",
        "BreezyByte3771",
        "BreezyGhost118",
        "BreezyMeteor8023",
        "BreezyPanther5004",
        "BreezyPenguin2154",
        "BreezyRaven6087",
        "BreezyRocket107",
        "BreezyWolf4416",
        "ChillBear1900",
        "ChillByte189",
        "ChillDragon7706",
        "ChillFalcon1777",
        "ChillFalcon7145",
        "ChillFlame1331",
        "ChillKnight5148",
        "ChillRobot1802",
        "ChillRobot9513",
        "CosmicBlaze1344",
        "CosmicFlame3734",
        "CosmicHawk8072",
        "CosmicPanther5281",
        "CosmicPenguin9544",
        "CosmicPirate7785",
        "CosmicWolf1536",
        "CrazyComet8646",
        "CrazyDolphin7957",
        "CrazyJaguar54",
        "CrazyJaguar6217",
        "CrazyLynx8901",
        "CrazyOtter7238",
        "CrazyPenguin5821",
        "CrazyPirate2848",
        "CrazyRobot1294",
        "CrazySky7778",
        "CyberBlaze6687",
        "CyberByte3938",
        "CyberCobra3998",
        "CyberFalcon2069",
        "CyberMeteor2345",
        "CyberMoon4782",
        "CyberMoon7268",
        "CyberOtter9454",
        "CyberStorm3920",
        "CyberWolf6372",
        "DizzyBlaze3627",
        "DizzyCobra4845",
        "DizzyFox8780",
        "DizzyKnight7450",
        "DizzyLion6212",
        "DizzyRobot7989",
        "DizzySky7750",
        "DizzyStorm7995",
        "ElectricFalcon5863",
        "ElectricFrost4296",
        "ElectricGhost2267",
        "ElectricPanther1147",
        "ElectricPanther2386",
        "ElectricSphinx8222",
        "EpicComet2648",
        "EpicFox410",
        "EpicFrost879",
        "EpicGriffin9978",
        "EpicJaguar2507",
        "EpicLynx846",
        "EpicMeteor3242",
        "EpicPanther4098",
        "EpicPhoenix646",
        "EpicRocket8541",
        "EpicWave8043",
        "EpicWizard4107",
        "FierceComet4051",
        "FierceDragon9799",
        "FierceGriffin3630",
        "FierceLion3021",
        "FierceLynx3502",
        "FierceLynx4872",
        "FierceNinja4907",
        "FierceSphinx7398",
        "FierceWolf1525",
        "FrostyCheetah5636",
        "FrostyDragon6225",
        "FrostyFlame7063",
        "FrostyFrost3831",
        "FrostyLynx1634",
        "FrostyMeteor421",
        "FrostyOtter8809",
        "FrostyPanda9406",
        "FrostyPanther5935",
        "FrostyPirate3140",
        "FrostySky9065",
        "FrostyStar4942",
        "FrostyTiger8857",
        "FunkyFox1042",
        "FunkyJaguar252",
        "FuzzyCheetah7435",
        "FuzzyDolphin8118",
        "FuzzyFlame6625",
        "FuzzyGriffin4378",
        "FuzzyOtter8424",
        "FuzzyPixel7634",
        "FuzzyRocket2548",
        "FuzzyRocket9768",
        "FuzzySpark2144",
        "FuzzyWolf1890",
        "FuzzyWolf8099",
        "GentleByte7306",
        "GentleFox8923",
        "GentleFrost1769",
        "GentleMeteor6544",
        "GentleMeteor9940",
        "GentlePhoenix1647",
        "GentleRobot8549",
        "GentleStorm8057",
        "GentleSun9708",
        "GoldenGhost9425",
        "GoldenGoblin5701",
        "GoldenMeteor6913",
        "GoldenPirate6659",
        "GoldenRocket7386",
        "GoldenShadow7023",
        "GoldenSun7963",
        "GoldenViper5353",
        "GoldenWave3849",
        "GrumpyBolt5199",
        "GrumpyDolphin1528",
        "GrumpyEagle7042",
        "GrumpyOtter2565",
        "GrumpyPenguin1480",
        "GrumpyShark3144",
        "GrumpyViper2069",
        "GrumpyViper5926",
        "GrumpyWave8904",
        "HappyByte7",
        "HappyFox9334",
        "HappyFrost8786",
        "HappyLion4565",
        "HappyMoon3250",
        "HappyPanda4333",
        "HappyRocket1085",
        "HappyRocket3110",
        "HappyShadow7518",
        "JazzyBear7150",
        "JazzyEagle6718",
        "JazzyJaguar8786",
        "JazzyNinja7658",
        "JazzyNova9441",
        "JazzySun4316",
        "JollyCheetah4233",
        "JollyFlame4375",
        "JollyFrost4089",
        "JollyFrost4326",
        "JollyGhost8280",
        "JollyGriffin4346",
        "JollyJaguar4199",
        "JollyLynx7286",
        "JollyMeteor3539",
        "JollyNova5156",
        "JollyOtter9081",
        "JollyPixel2168",
        "JollyRocket8999",
        "JollyShadow8180",
        "JollyTiger9490",
        "JollyWave5515",
        "LoyalComet9578",
        "LoyalDragon2268",
        "LoyalFalcon1122",
        "LoyalFox7452",
        "LoyalPanther6072",
        "LoyalShark6368",
        "LoyalSun4011",
        "LoyalWave3296",
        "LuckyBlaze8752",
        "LuckyCheetah1128",
        "LuckyComet1892",
        "LuckyFalcon5051",
        "LuckyFlame4916",
        "LuckyJaguar2978",
        "LuckyJaguar8703",
        "LuckyPanther3007",
        "LuckySky6898",
        "LuckyViper7179",
        "MightyCheetah3899",
        "MightyDolphin7382",
        "MightyDolphin8032",
        "MightyDragon3873",
        "MightyFlame8890",
        "MightyFlame9087",
        "MightyFox1233",
        "MightyJaguar9225",
        "MightyNova804",
        "MightyRaven4346",
        "MightySphinx3131",
        "MightySphinx8822",
        "MightyViper4892",
        "NeonComet2288",
        "NeonEagle9222",
        "NeonGhost6276",
        "NeonLynx8825",
        "NeonRaven3123",
        "NeonRocket4617",
        "NeonShadow91",
        "NeonShark8851",
        "NeonStorm2622",
        "NeonWave3697",
        "NeonWizard4862",
        "NimbleHawk6538",
        "NimbleHawk8667",
        "NimblePanther9190",
        "NimblePixel2074",
        "NimbleRaven6326",
        "NimbleViper3842",
        "PeppyBolt1984",
        "PeppyGhost4335",
        "PeppyHawk3681",
        "PeppyPanther9772",
        "PeppyPenguin8921",
        "PeppyPixel4372",
        "PeppyPixel6227",
        "PeppyShadow1613",
        "PeppySky2852",
        "PeppySky8936",
        "PeppySun1877",
        "PeppySun3229",
        "PeppySun6512",
        "PeppyWave2241",
        "PluckyBear937",
        "PluckyDolphin2878",
        "PluckyLion6746",
        "PluckyLynx9039",
        "PluckyPixel999",
        "PluckyRocket4013",
        "QuantumByte8699",
        "QuantumCheetah5447",
        "QuantumComet5547",
        "QuantumKnight6571",
        "QuantumLynx5932",
        "QuantumPirate7750",
        "QuantumSphinx7246",
        "QuantumViper5550",
        "QuantumWolf9199",
        "RadiantFalcon8544",
        "RadiantNova4931",
        "RadiantRaven3946",
        "RadiantSky1626",
        "RadiantSky2533",
        "RadiantSpark1194",
        "RadiantStar6513",
        "RadiantWave8168",
        "RapidComet6485",
        "RapidFox7479",
        "RapidJaguar9032",
        "RapidMeteor9741",
        "RapidMoon2595",
        "RapidNinja2901",
        "RapidNova1291",
        "RapidPenguin1628",
        "RapidPenguin4670",
        "RapidRaven9371",
        "RapidRocket7954",
        "RapidShark7759",
        "RapidShark9251",
        "RapidSky9198",
        "RapidSphinx6752",
        "RapidStorm4709",
        "RapidWave7614",
        "RetroFlame5492",
        "RetroGoblin2781",
        "RetroHawk6692",
        "RetroMeteor1787",
        "RetroMeteor5440",
        "RetroMoon4480",
        "RetroPhoenix4890",
        "RetroPixel8091",
        "RetroSphinx2580",
        "RetroWave2928",
        "RetroYeti1490",
        "RetroYeti4115",
        "RoyalFalcon5169",
        "RoyalFox3156",
        "RoyalFox7561",
        "RoyalFrost5314",
        "RoyalJaguar4162",
        "RoyalLion2418",
        "RoyalOtter515",
        "RoyalPhoenix6228",
        "RoyalRocket7141",
        "RoyalShadow2442",
        "RoyalShark1680",
        "RoyalSky1481",
        "RoyalWizard7542",
        "RoyalWolf3165",
        "RustyByte225",
        "RustyDragon4067",
        "RustyFlame1698",
        "RustyJaguar511",
        "RustyLynx547",
        "RustyNova6150",
        "RustyPirate950",
        "RustyViper3444",
        "ShinyCobra9698",
        "ShinyFalcon5315",
        "ShinyFox5948",
        "ShinySky8838",
        "ShinyStorm9976",
        "ShinyTiger5465",
        "SilentDolphin6679",
        "SilentDragon5278",
        "SilentFrost3854",
        "SilentGriffin5591",
        "SilentLion345",
        "SilentLynx9017",
        "SilentMeteor3979",
        "SilentMoon965",
        "SilentRaven6253",
        "SilentRocket6799",
        "SilentSpark4574",
        "SilentWave8728",
        "SlyFlame6212",
        "SlyFlame9563",
        "SlyGhost7360",
        "SlyGoblin7737",
        "SlyLion5261",
        "SlyMeteor2532",
        "SlyPixel8948",
        "SlyRaven9570",
        "SlyShadow1623",
        "SlyTiger3389",
        "SlyWizard1546",
        "SlyWolf2939",
        "SnazzyBear7619",
        "SnazzyCheetah993",
        "SnazzyDragon5384",
        "SnazzyEagle1226",
        "SnazzyOtter660",
        "SnazzyOtter7607",
        "SnazzyRocket8818",
        "SneakyComet859",
        "SneakyHawk232",
        "SneakyOtter9750",
        "SneakyStar8487",
        "SpicyBlaze6210",
        "SpicyBlaze987",
        "SpicyFalcon2871",
        "SpicyFalcon5689",
        "SpicyFlame445",
        "SpicyFrost2951",
        "SpicyRaven3827",
        "SpicySphinx5184",
        "SpicyWizard1140",
        "StormyBear8006",
        "StormyBolt1877",
        "StormyBolt3698",
        "StormyCheetah3589",
        "StormyComet424",
        "StormyComet712",
        "StormyGhost8000",
        "StormyGriffin1895",
        "StormyGriffin2237",
        "StormyJaguar8670",
        "StormyKnight5618",
        "StormyPanda4543",
        "StormyPanther3471",
        "StormyPanther5664",
        "StormySky8139",
        "StormySky9326",
        "StormyStorm2804",
        "StormyWolf9196",
        "SturdyByte602",
        "SturdyKnight3458",
        "SturdyLynx8271",
        "SturdyNinja6925",
        "SturdyNinja8838",
        "SturdySpark1747",
        "SturdySphinx9778",
        "SturdyStar3034",
        "SturdyTiger1797",
        "SturdyWolf4052",
        "SunnyFalcon3793",
        "SunnyFlame6562",
        "SunnyGhost5913",
        "SunnyGhost6626",
        "SunnyGoblin7844",
        "SunnyLynx2038",
        "SunnyMoon4633",
        "SunnyPhoenix3181",
        "SunnySpark3771",
        "TurboByte6819",
        "TurboJaguar1234",
        "TurboJaguar1502",
        "TurboOtter6566",
        "TurboPhoenix3847",
        "TurboPirate3682",
        "TurboRaven1292",
        "TurboShadow2523",
        "TurboShark743",
        "TurboViper7124",
        "VibrantByte6659",
        "VibrantCobra8432",
        "VibrantDolphin716",
        "VibrantGoblin7617",
        "VibrantNinja5097",
        "VibrantPanther3586",
        "VibrantPenguin2750",
        "VibrantShadow5574",
        "VibrantShark2104",
        "VibrantSpark917",
        "VibrantSphinx10",
        "VibrantSphinx6233",
        "VibrantViper1308",
        "VividBear1189",
        "VividComet154",
        "VividDolphin889",
        "VividDolphin9432",
        "VividDragon5154",
        "VividHawk3264",
        "VividPenguin8851",
        "VividStar4983",
        "VividSun888",
        "VividTiger9133",
        "VividViper6931",
        "VividViper8956",
        "WickedFalcon7712",
        "WickedGoblin1748",
        "WickedGoblin97",
        "WickedJaguar5273",
        "WickedNova3115",
        "WildCheetah3674",
        "WildCobra5560",
        "WildComet6212",
        "WildComet7828",
        "WildDragon6940",
        "WildGoblin6581",
        "WildGriffin1313",
        "WildMeteor7812",
        "WildNinja5040",
        "WildNinja6210",
        "WildViper5874",
        "WittyBolt7700",
        "WittyCobra4461",
        "WittyEagle3506",
        "WittyEagle8939",
        "WittyFrost1170",
        "WittyGoblin8042",
        "WittyHawk6690",
        "WittyJaguar1167",
        "WittyPirate5017",
        "WittyRocket9107",
        "WittySun9106",
        "WittyViper360",
        "WittyWolf9433",
        "WittyYeti1990",
        "ZestyCobra4498",
        "ZestyDragon4650",
        "ZestyFox4921",
        "ZestyFox9241",
        "ZestyGoblin9008",
        "ZestyKnight9784",
        "ZestyLion5144",
        "ZestyMeteor6047",
        "ZestyMoon7974",
        "ZestyPhoenix2635",
        "ZestyPhoenix5795",
        "ZestyShadow2493",
        "ZestyShadow6119",
        "ZestySky4066",
        "ZestyWolf2160",
    };
    private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true
    };

    private string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private string CreateJwt(string sub, string name, bool isJunior = false, int registrationStatus = 0, bool isNew = true)
    {
        var header = new { alg = "HS256", typ = "JWT" };
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = new
        {
            sub,
            name,
            unique_name = name,
            preferred_username = name,
            nickname = name,
            role = "player",
            account_id = sub,
            accountId = sub,
            sid = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
            junior = isJunior,
            isJunior = isJunior,
            registrationStatus = registrationStatus,
            isNew = isNew,
            iat = now,
            exp = now + 86400,
            iss = "https://accounts.rec.net",
            aud = "recroom",
            auth_time = now,
            jti = Guid.NewGuid().ToString()
        };
        var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header)));
        var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        var signingInput = headerB64 + "." + payloadB64;
        using var hmac = new HMACSHA256(_jwtSecret);
        var sig = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput)));
        return signingInput + "." + sig;
    }

    private List<JsonElement> LoadAccounts()
    {
        var accounts = Rex_Room_20230414.API.AccountHelper.LoadAccounts();
        string json = JsonSerializer.Serialize(accounts);
        return JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new List<JsonElement>();
    }

    private void SaveAccounts(List<JsonElement> accounts)
    {
        var list = JsonSerializer.Deserialize<List<Account>>(JsonSerializer.Serialize(accounts));
        if (list != null) Rex_Room_20230414.API.AccountHelper.SaveAccounts(list);
    }

    public string Response(HttpContext context)
    {
        try
        {
            string body = "";
            if (context.Request.Body != null)
            {
                using var reader = new StreamReader(context.Request.Body);
                body = reader.ReadToEndAsync().Result ?? "";
            }
            Console.WriteLine($"[DEBUG /connect/token] Body: '{body}' ContentType: '{context.Request.ContentType}'");

            string grantType = "", username = "", password = "", token = "";
            string platformId = "", accountIdStr = "", asid = "", deviceId = "", refreshTokenParam = "";

            if (context.Request.HasFormContentType && !string.IsNullOrEmpty(body))
            {
                var parsed = System.Web.HttpUtility.ParseQueryString(body);
                grantType = parsed["grant_type"] ?? "";
                username = parsed["username"] ?? "";
                password = parsed["password"] ?? "";
                token = parsed["token"] ?? "";
                platformId = parsed["platform_id"] ?? "";
                accountIdStr = parsed["account_id"] ?? "";
                asid = parsed["asid"] ?? "";
                deviceId = parsed["device_id"] ?? "";
                refreshTokenParam = parsed["refresh_token"] ?? "";
            }
            else if (!string.IsNullOrEmpty(body) && body.TrimStart().StartsWith("{"))
            {
                try
                {
                    var json = System.Text.Json.JsonDocument.Parse(body);
                    var root = json.RootElement;
                    if (root.TryGetProperty("grant_type", out var jgt)) grantType = jgt.GetString() ?? "";
                    if (root.TryGetProperty("username", out var ju)) username = ju.GetString() ?? "";
                    if (root.TryGetProperty("password", out var jp)) password = jp.GetString() ?? "";
                    if (root.TryGetProperty("token", out var jt)) token = jt.GetString() ?? "";
                    if (root.TryGetProperty("platform_id", out var jpi)) platformId = jpi.GetString() ?? "";
                    if (root.TryGetProperty("account_id", out var jai)) accountIdStr = jai.GetString() ?? "";
                    if (root.TryGetProperty("asid", out var ja)) asid = ja.GetString() ?? "";
                    if (root.TryGetProperty("device_id", out var jd)) deviceId = jd.GetString() ?? "";
                    if (root.TryGetProperty("refresh_token", out var jrt)) refreshTokenParam = jrt.GetString() ?? "";
                }
                catch { }
            }
            if (string.IsNullOrEmpty(grantType)) grantType = context.Request.Query["grant_type"].FirstOrDefault() ?? "";
            if (string.IsNullOrEmpty(platformId)) platformId = context.Request.Query["platform_id"].FirstOrDefault() ?? "";
            if (string.IsNullOrEmpty(accountIdStr)) accountIdStr = context.Request.Query["account_id"].FirstOrDefault() ?? "";
            if (string.IsNullOrEmpty(asid)) asid = context.Request.Query["asid"].FirstOrDefault() ?? "";
            if (string.IsNullOrEmpty(deviceId)) deviceId = context.Request.Query["device_id"].FirstOrDefault() ?? "";

            Console.WriteLine($"[DEBUG /connect/token] grant_type='{grantType}' platform_id='{platformId}' account_id='{accountIdStr}' token='{token}'");

            lock (_lock)
            {
                var accounts = LoadAccounts();
                string sub = "";
                string displayName = "";
                string accessToken = "";

                if (string.IsNullOrEmpty(sub) && grantType != "create_account" && !string.IsNullOrEmpty(accountIdStr) && int.TryParse(accountIdStr, out int requestedId))
                {
                    var acc = accounts.FirstOrDefault(a =>
                    {
                        if (a.TryGetProperty("Id", out var id)) return id.GetInt32() == requestedId;
                        return false;
                    });
                    if (acc.ValueKind != JsonValueKind.Undefined)
                    {
                        sub = acc.GetProperty("Id").GetInt32().ToString();
                        displayName = acc.TryGetProperty("DisplayName", out var dn) && dn.GetString() is string s ? s : "Player";
                    }
                }

                if (string.IsNullOrEmpty(sub) && grantType != "create_account" && !string.IsNullOrEmpty(platformId))
                {
                    var acc = accounts
                        .Where(a =>
                        {
                            if (a.TryGetProperty("PlatformId", out var p)) return p.GetString() == platformId;
                            return false;
                        })
                        .OrderByDescending(a =>
                        {
                            if (a.TryGetProperty("Id", out var id)) return id.GetInt32();
                            return 0;
                        })
                        .FirstOrDefault();
                    if (acc.ValueKind != JsonValueKind.Undefined)
                    {
                        sub = acc.GetProperty("Id").GetInt32().ToString();
                        displayName = acc.TryGetProperty("DisplayName", out var dn) && dn.GetString() is string s ? s : "Player";
                    }
                }

                if (string.IsNullOrEmpty(sub) && grantType == "password" && !string.IsNullOrEmpty(username))
                {
                    var acc = accounts.FirstOrDefault(a =>
                    {
                        var u = a.TryGetProperty("Username", out var un) ? un.GetString() : "";
                        var p = a.TryGetProperty("Password", out var pw) ? pw.GetString() : "";
                        if (u == username && (string.IsNullOrEmpty(p) || p == password))
                            return true;
                        if (int.TryParse(username, out int uid))
                        {
                            var aId = a.TryGetProperty("Id", out var idProp) ? idProp.GetInt32() : 0;
                            if (aId == uid) return true;
                        }
                        return false;
                    });
                    if (acc.ValueKind != JsonValueKind.Undefined)
                    {
                        sub = acc.GetProperty("Id").GetInt32().ToString();
                        displayName = acc.TryGetProperty("DisplayName", out var dn) && dn.GetString() is string s ? s : username;
                    }
                }

                if (string.IsNullOrEmpty(sub) && !string.IsNullOrEmpty(refreshTokenParam))
                {
                    var acc = accounts.FirstOrDefault(a =>
                    {
                        if (a.TryGetProperty("RefreshToken", out var rt)) return rt.GetString() == refreshTokenParam;
                        return false;
                    });
                    if (acc.ValueKind != JsonValueKind.Undefined)
                    {
                        sub = acc.GetProperty("Id").GetInt32().ToString();
                        displayName = acc.TryGetProperty("DisplayName", out var dn) && dn.GetString() is string s ? s : "Player";
                        Console.WriteLine($"[DEBUG /connect/token] Matched by refresh_token, account id={sub}");
                    }
                }

                if (string.IsNullOrEmpty(sub) && grantType == "create_account" && !string.IsNullOrEmpty(platformId))
                {
                    int nextId = accounts.Any()
                        ? accounts.Max(a => a.TryGetProperty("Id", out var id) && id.TryGetInt32(out var i) ? i : 0) + 1
                        : 1;
                    var newAccount = new Account
                    {
                        Id = nextId,
                        PlatformId = platformId,
                        Username = "",
                        Password = "",
                        DisplayName = "",
                        Token = "",
                        RefreshToken = "",
                        IsNew = true,
                        Level = 1,
                        Experience = 0,
                        Tokens = 1000,
                        Gold = 500,
                        ProfileImage = null,
                        Bio = "",
                        LastLoginTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"),
                        CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"),
                        IsJunior = false,
                        RegistrationStatus = 0,
                        PersonalPronouns = 0,
                        IdentityFlags = 0,
                        DisplayEmoji = null
                    };
                    Rex_Room_20230414.Database.DatabaseManager.SavePlayerData(newAccount);
                    accounts.Add(JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(newAccount)));
                    sub = nextId.ToString();
                    displayName = "";

                    // Reference: auth.app.ts seeds the new account into Orientation
                    // (room 13, instance -2, photon rec.13) at the end of account
                    // creation, so the first heartbeat echoes the instance the
                    // client already tagged itself with.
                    RoomData.AssignOrientation(nextId);

                    Console.WriteLine($"[DEBUG /connect/token] Created incomplete account id={nextId} (registrationStatus=0) for platform_id='{platformId}' grant_type='create_account'");
                }

                if (string.IsNullOrEmpty(sub))
                {
                    context.Response.StatusCode = 400;
                    Console.WriteLine($"[DEBUG /connect/token] 400 invalid_grant: no account found for grant_type='{grantType}' platform_id='{platformId}' account_id='{accountIdStr}'");
                    return "{\"error\":\"invalid_grant\",\"error_description\":\"Account not found\"}";
                }

                if (grantType == "refresh_token")
                {
                    if (!string.IsNullOrEmpty(refreshTokenParam))
                    {
                        var acc = accounts.FirstOrDefault(a =>
                        {
                            if (a.TryGetProperty("Id", out var id)) return id.GetInt32().ToString() == sub;
                            return false;
                        });
                        if (acc.ValueKind != JsonValueKind.Undefined)
                        {
                            sub = acc.GetProperty("Id").GetInt32().ToString();
                            displayName = acc.TryGetProperty("DisplayName", out var dn) && dn.GetString() is string s ? s : "Player";
                        }
                    }
                }

                bool isJunior = false;
                int registrationStatus = 0;
                bool isNew = true;
                var matched = accounts.FirstOrDefault(a =>
                {
                    if (a.TryGetProperty("Id", out var id)) return id.GetInt32().ToString() == sub;
                    return false;
                });
                if (matched.ValueKind != JsonValueKind.Undefined)
                {
                    if (matched.TryGetProperty("IsJunior", out var j)) isJunior = j.ValueKind == JsonValueKind.True;
                    if (matched.TryGetProperty("RegistrationStatus", out var rs) && rs.TryGetInt32(out var rsv)) registrationStatus = rsv;
                    if (matched.TryGetProperty("IsNew", out var inew)) isNew = inew.ValueKind == JsonValueKind.True;
                }

                string accUsername = "";
                string accDisplayName = "";
                string accBirthday = "";
                if (matched.ValueKind != JsonValueKind.Undefined)
                {
                    if (matched.TryGetProperty("Username", out var un)) accUsername = un.GetString() ?? "";
                    if (matched.TryGetProperty("DisplayName", out var dn)) accDisplayName = dn.GetString() ?? "";
                    if (matched.TryGetProperty("Birthday", out var bd)) accBirthday = bd.GetString() ?? "";
                }
                Console.WriteLine($"[DEBUG /connect/token] DECISION login: accountId={sub} grantType='{grantType}' username='{accUsername}' displayName='{accDisplayName}' birthday='{accBirthday}' registrationStatus={registrationStatus} isNew={isNew}");

                if (registrationStatus == 0 && grantType != "password" && grantType != "create_account" && grantType != "cached_login" && grantType != "refresh_token")
                {
                    context.Response.StatusCode = 400;
                    Console.WriteLine($"[DEBUG /connect/token] 400 incomplete_account: account {sub} has registrationStatus=0 (not completed). Rejecting token for grant_type='{grantType}'.");
                    return "{\"error\":\"invalid_grant\",\"error_description\":\"Account registration not completed\"}";
                }

                string jwtToken = CreateJwt(sub, displayName, isJunior, registrationStatus, isNew);
                string newRefreshToken = Guid.NewGuid().ToString();

                try
                {
                    var accToUpdate = accounts.FirstOrDefault(a =>
                    {
                        if (a.TryGetProperty("Id", out var id)) return id.GetInt32().ToString() == sub;
                        return false;
                    });
                    if (accToUpdate.ValueKind != JsonValueKind.Undefined)
                    {
                        accToUpdate.TryGetProperty("Username", out var un);
                        var updated = new Account
                        {
                            Id = int.Parse(sub),
                            PlatformId = accToUpdate.TryGetProperty("PlatformId", out var pid) ? pid.GetString() : platformId,
                            Username = un.GetString() ?? displayName,
                            Password = accToUpdate.TryGetProperty("Password", out var pw) ? pw.GetString() : "",
                            DisplayName = accToUpdate.TryGetProperty("DisplayName", out var dn2) && dn2.GetString() is string dn2s ? dn2s : displayName,
                            Token = jwtToken,
                            RefreshToken = newRefreshToken,
                            IsNew = isNew,
                            Level = accToUpdate.TryGetProperty("Level", out var lv) && lv.TryGetInt32(out var lvi) ? lvi : 1,
                            Experience = accToUpdate.TryGetProperty("Experience", out var ex) && ex.TryGetInt32(out var exi) ? exi : 0,
                            Tokens = accToUpdate.TryGetProperty("Tokens", out var tk) && tk.TryGetInt32(out var tki) ? tki : 1000,
                            Gold = accToUpdate.TryGetProperty("Gold", out var gd) && gd.TryGetInt32(out var gdi) ? gdi : 500,
                            ProfileImage = accToUpdate.TryGetProperty("ProfileImage", out var pi) ? pi.GetString() : null,
                            Bio = accToUpdate.TryGetProperty("Bio", out var bi) ? bi.GetString() : "",
                            LastLoginTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"),
                            CreatedAt = accToUpdate.TryGetProperty("CreatedAt", out var ca) ? ca.GetString() : DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"),
                            IsJunior = isJunior,
                            RegistrationStatus = registrationStatus,
                            PersonalPronouns = accToUpdate.TryGetProperty("PersonalPronouns", out var pp) && pp.TryGetInt32(out var ppi) ? ppi : 0,
                            IdentityFlags = accToUpdate.TryGetProperty("IdentityFlags", out var iff) && iff.TryGetInt32(out var iffi) ? iffi : 0,
                            DisplayEmoji = accToUpdate.TryGetProperty("DisplayEmoji", out var de) ? de.GetString() : null,
                            Birthday = accToUpdate.TryGetProperty("Birthday", out var bd) ? bd.GetString() : null,
                            Email = accToUpdate.TryGetProperty("Email", out var em) ? em.GetString() : null,
                            Phone = accToUpdate.TryGetProperty("Phone", out var ph) ? ph.GetString() : null,
                            OutfitSelections = accToUpdate.TryGetProperty("OutfitSelections", out var os) ? os.GetString() : null,
                            OutfitSelectionsV2 = accToUpdate.TryGetProperty("OutfitSelectionsV2", out var os2) ? os2.GetString() : null,
                            FaceFeatures = accToUpdate.TryGetProperty("FaceFeatures", out var ff) ? ff.GetString() : null,
                            HairColor = accToUpdate.TryGetProperty("HairColor", out var hc) ? hc.GetString() : null,
                            SkinColor = accToUpdate.TryGetProperty("SkinColor", out var sc) ? sc.GetString() : null,
                            HasSetAvatar = accToUpdate.TryGetProperty("HasSetAvatar", out var hsa) && hsa.ValueKind == JsonValueKind.True,
                            // Must be carried over, otherwise this whole-account
                            // rebuild on every login silently resets the wardrobe
                            // to "owns nothing" and the client then refuses to
                            // let the player leave the dorm with "You're wearing
                            // items you don't own".
                            UnlockedAvatarItemIds = accToUpdate.TryGetProperty("UnlockedAvatarItemIds", out var uai) && uai.ValueKind == JsonValueKind.Array
                                ? uai.EnumerateArray()
                                    .Where(e => e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out _))
                                    .Select(e => e.GetInt32()).ToList()
                                : new List<int>(),
                            // These three were missing from this whole-account rebuild,
                            // so every login silently reset them to their defaults:
                            // unopened gift boxes vanished, inventions were lost, and
                            // the dorm reward could pay out again on the same login.
                            OwnedInventionIds = accToUpdate.TryGetProperty("OwnedInventionIds", out var oii) && oii.ValueKind == JsonValueKind.Array
                                ? oii.EnumerateArray()
                                    .Where(e => e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out _))
                                    .Select(e => e.GetInt32()).ToList()
                                : new List<int>(),
                            LastRewardLoginLock = accToUpdate.TryGetProperty("LastRewardLoginLock", out var lrl) ? lrl.GetString() : null,
                            LastRewardAt = accToUpdate.TryGetProperty("LastRewardAt", out var lra) ? lra.GetString() : null,
                            GiftPackages = accToUpdate.TryGetProperty("GiftPackages", out var gp) && gp.ValueKind == JsonValueKind.Array
                                ? System.Text.Json.JsonSerializer.Deserialize<List<API.GiftPackage>>(gp.GetRawText()) ?? new List<API.GiftPackage>()
                                : new List<API.GiftPackage>()
                        };
                        Rex_Room_20230414.Database.DatabaseManager.SavePlayerData(updated);
                    }
                }
                catch (Exception saveEx)
                {
                    Console.WriteLine($"[DEBUG /connect/token] Failed to persist account: {saveEx.Message}");
                }

                var responseObj = new
                {
                    access_token = jwtToken,
                    accessToken = jwtToken,
                    token_type = "Bearer",
                    tokenType = "Bearer",
                    expires_in = 86400,
                    expiresIn = 86400,
                    refresh_token = newRefreshToken,
                    refreshToken = newRefreshToken,
                    expires = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    issued = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    scope = "offline_access profile rn rn.accounts rn.accounts.gc rn.api rn.chat rn.clubs rn.commerce rn.match.read rn.match.write rn.notify rn.rooms rn.storage",
                    accountId = sub,
                    userId = sub,
                    asid = asid,
                    deviceId = deviceId,
                    key = "8oQ+e+WQaOBPbEcakhqs3dwZZdOmmyDUmJSD9u4AHMY="
                };
                string response = JsonSerializer.Serialize(responseObj);
                Console.WriteLine($"[DEBUG /connect/token] Response: {response}");
                return response;
            }
        }
        catch (Exception ex)
        {
            return "{\"error\":\"" + ex.Message.Replace("\"", "'") + "\"}";
        }
    }

    public string GetUrl() => "/connect/token";
    public string GetMethod() => HttpMethods.Post;
}