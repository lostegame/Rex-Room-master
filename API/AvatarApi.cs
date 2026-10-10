namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

public class AvatarApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context);
        var acc = accountId != null ? AccountHelper.GetAccountById(accountId.Value) : null;

        // Base face features (no hair/clothing selected yet for a new account).
        string faceFeatures = "{\"ver\":7,\"eyeId\":\"AjGMoJhEcEehacRZjUMuDg\",\"eyePos\":{\"x\":0.0,\"y\":0.0},\"eyeScl\":0.0,\"mouthId\":\"FrZBRanXEEK29yKJ4jiMjg\",\"mouthPos\":{\"x\":0.0,\"y\":0.0},\"mouthScl\":0.0,\"hairPrimaryColorId\":\"\",\"hairSecondaryColorId\":\"\",\"hairPatternId\":\"\",\"beardColorId\":\"\",\"beardSecondaryColorId\":\"\",\"beardPatternId\":\"\",\"faceShapeId\":\"yR4oYZr_AUSynXCgwS2lGw\",\"bodyShapeId\":\"bY1RGIph0kiAxbd6Shn9tQ\",\"useHatAnchorParams\":true,\"useHelmetHair\":1,\"hideEars\":false,\"baseAvatarType\":\"\"}";

        string skinColor = "85343b16-d58a-4091-96d8-083a81fb03ae";
        string hairColor = "";

        bool hasSavedOutfit = acc != null && acc.HasSetAvatar;

        // For a brand-new/incomplete account: NO outfit pre-applied. The character-creation
        // screen drives the first POST /api/avatar/v2/set itself.
        string outfitSelections = "";
        string outfitSelectionsV2 = "{\"selections\":[]}";

        if (hasSavedOutfit)
        {
            outfitSelections = acc.OutfitSelections ?? "";
            if (!string.IsNullOrEmpty(acc.OutfitSelectionsV2))
                outfitSelectionsV2 = acc.OutfitSelectionsV2;
            if (!string.IsNullOrEmpty(acc.FaceFeatures))
                faceFeatures = acc.FaceFeatures;
            hairColor = acc.HairColor ?? "";
        }

        string outfitV2Json = JsonSerializer.Serialize(outfitSelectionsV2);
        string faceJson = JsonSerializer.Serialize(faceFeatures);

        return "{\"IsModified\":false,\"IsDrawingDisabled\":false,\"SlotPreviewImageName\":null," +
               "\"OutfitSelections\":\"" + JsonEscape(outfitSelections) + "\"," +
               "\"OutfitSelectionsV2\":" + outfitV2Json + "," +
               "\"FaceFeatures\":" + faceJson + "," +
               "\"SkinColor\":\"" + skinColor + "\"," +
               "\"HairColor\":\"" + JsonEscape(hairColor) + "\"," +
               "\"CustomAvatarItems\":[]}";
    }

    private static string JsonEscape(string s) =>
        (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

    public string GetUrl() => "/api/avatar/v2";
    public string GetMethod() => HttpMethods.Get;
}