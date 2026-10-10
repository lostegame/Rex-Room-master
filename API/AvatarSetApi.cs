namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

// POST /api/avatar/v2/set — client saves/hands the outfit. Persist it so GET /api/avatar/v2 returns it.
public class AvatarSetApi : IApi
{
    public string Response(HttpContext context)
    {
        string body = "";
        if (context.Request.Body != null)
        {
            using var reader = new StreamReader(context.Request.Body);
            body = reader.ReadToEndAsync().Result ?? "";
        }

        var accountId = AccountHelper.GetAccountIdFromToken(context);
        if (accountId != null)
        {
            var accounts = AccountHelper.LoadAccounts();
            var acc = accounts.FirstOrDefault(a => a.Id == accountId.Value);
            if (acc != null && !string.IsNullOrEmpty(body))
            {
                try
                {
                    var json = JsonDocument.Parse(body);
                    var root = json.RootElement;
                    if (root.TryGetProperty("OutfitSelections", out var os)) acc.OutfitSelections = os.GetString() ?? "";
                    if (root.TryGetProperty("OutfitSelectionsV2", out var os2)) acc.OutfitSelectionsV2 = os2.GetString() ?? "";
                    if (root.TryGetProperty("FaceFeatures", out var ff)) acc.FaceFeatures = ff.GetString() ?? "";
                    if (root.TryGetProperty("HairColor", out var hc)) acc.HairColor = hc.GetString() ?? "";
                    if (root.TryGetProperty("SkinColor", out var sc)) acc.SkinColor = sc.GetString() ?? "";
                    acc.HasSetAvatar = true;
                    AccountHelper.SaveAccounts(accounts);

                    // Equipping must also GRANT ownership of the non-free pieces
                    // that were just put on. /econ/customAvatarItems/v1/owned is
                    // built purely from UnlockedAvatarItemIds, so without this the
                    // wardrobe stayed empty for an item the player is actually
                    // wearing: the client then hides the piece (clothes never
                    // render) and also refuses to let the player take the matching
                    // spawned item out of a gift box.
                    int grantedNow = CustomAvatarItemsOwnedApi.GrantWornItems(acc);
                    if (grantedNow > 0)
                    {
                        AccountHelper.SaveAccount(acc);
                        Console.WriteLine($"[Avatar/v2/set] granted ownership of {grantedNow} worn item(s) for account {acc.Id}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Avatar/v2/set] persist error: {ex.Message}");
                }
            }
        }

        // Echo the sent state back (reference #28 echoes full avatar state).
        return string.IsNullOrEmpty(body) ? "{\"Success\":true}" : body;
    }

    public string GetUrl() => "/api/avatar/v2/set";
    public string GetMethod() => HttpMethods.Post;
}