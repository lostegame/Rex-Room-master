namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;

// Custom avatar items (the Maker-made badges/gizmos browsers).
//
// Reference shapes (rex room LogOutput.log):
//   GET /api/customAvatarItems/v1/featured -> []  (empty list)
//   GET /api/customAvatarItems/v1/hot      -> [{ "CurrentSaves":[],"Tags":[],
//        "CustomBadgeMetadata":null,"RankingContext":null,"PurchaseInfo":null,
//        "CustomAvatarItemId":"<guid>","CreatorAccountId":N,"Name":"...",
//        "Description":"...","Price":N,"Accessibility":1,"ForceCannotPublish":false,
//        "IsFeatured":..., ... }]
//   GET /api/customAvatarItems/v1/minPriceForPublicItem -> 100   (a bare number)
//
// featured mirrors the reference and stays empty; minPriceForPublicItem answers the
// literal 100 the reference returns. All three were falling through to the generic
// 200 {"Success":true}, which is an object where a list/number is expected.
public class CustomAvatarItemsApi : IApi
{
    private readonly string _url;
    private readonly string _kind; // featured | hot | minPrice

    public CustomAvatarItemsApi(string url, string kind)
    {
        _url = url;
        _kind = kind;
    }

    public string Response(HttpContext context)
    {
        switch (_kind)
        {
            case "minPrice":
                // Bare JSON number, not an object.
                return "100";

            case "featured":
                // The reference itself returns an empty list here.
                return "[]";

            default:
                return "[]";
        }
    }

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
}
