namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;

// GET /api/inventions/v2/mine
//
// This is the player's INVENTORY: the inventions they actually bought. It used
// to filter the catalog by CreatorPlayerId, which is a different concept and the
// reason un-purchased inventions could render as OWNED. Ownership now lives in
// PlayerEconomy.OwnedInventionIds, so "mine" and the OWNED badge on a tile are
// driven by the same list and cannot disagree.
public class InventionsMineV2Api : IApi
{
    public string Response(HttpContext context)
    {
        int accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;

        var owned = new System.Collections.Generic.HashSet<int>(
            PlayerEconomy.OwnedInventions(accountId));

        var sb = new StringBuilder("[");
        bool first = true;
        foreach (var i in InventionCatalog.Page(0, 100))
        {
            if (!owned.Contains(i.InventionId)) continue;
            if (!first) sb.Append(',');
            first = false;
            sb.Append(InventionCatalog.Dto(i, accountId));
        }
        sb.Append(']');
        return sb.ToString();
    }

    public string GetUrl() => "/api/inventions/v2/mine";
    public string GetMethod() => HttpMethods.Get;
}
