namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;

// GET /rooms/favoritedby/me and /roomserver/rooms/favoritedby/me
//
// Reference returns a BARE ARRAY of room DTOs (not a paged {"Results":[...]}):
//   /rooms/visitedby/me       -> [{RoomId:257,...}]
//   /rooms/favoritedby/me     -> []
//   /rooms/createdby/me       -> [{...}]
//   /rooms/hot                -> {"Results":[...],"TotalResults":N}
//
// Favorites come from RoomInteractionStore so the list agrees with what the
// favorite toggle wrote, instead of always being empty.
public class RoomFavoritedByMeApi : IApi
{
    private readonly string _url;
    public RoomFavoritedByMeApi(string url = "/roomserver/rooms/favoritedby/me") { _url = url; }

    public string Response(HttpContext context)
    {
        int accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        var list = new System.Collections.Generic.List<System.Text.Json.JsonElement>();
        foreach (int roomId in RoomInteractionStore.RoomsFavoritedBy(accountId))
            if (RoomCatalog.TryGetRoom(roomId, out var el))
                list.Add(el);
        return RoomCatalog.ListJson(list.ToArray(), accountId);
    }

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
}
