namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;

// GET /api/relationships/v6/current/friends — bare array of relationship rows.
public class RelationshipsFriendsApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        return SocialStore.FriendsJson(accountId);
    }

    public string GetUrl() => "/api/relationships/v6/current/friends";
    public string GetMethod() => HttpMethods.Get;
}

// GET /api/relationships/v1/relationshipwith/{playerId} — the relationship row
// between the caller and {playerId}, or null when none exists. Anti-enumeration
// is not a concern here: the 2023 client drives the social panel from this.
public class RelationshipWithApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        string path = context.Request.Path.Value ?? "";
        var segs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        int otherId = 0;
        if (segs.Length > 0) int.TryParse(segs[segs.Length - 1], out otherId);
        if (otherId == 0) return "null";

        foreach (var r in SocialStore.GetRelationships(accountId))
        {
            if (r.PlayerID == otherId)
                return $@"{{""Id"":{r.Id},""PlayerID"":{r.PlayerID},""RelationshipType"":{r.RelationshipType},"
                     + $@"""Favorited"":{r.Favorited},""Muted"":{r.Muted},""Ignored"":{r.Ignored}}}";
        }
        return "null";
    }

    public string GetUrl() => "/api/relationships/v1/relationshipwith/{playerId}";
    public string GetMethod() => HttpMethods.Get;
}

// POST /api/relationships/v2/sendfriendrequest — shows the target a Message of
// type 4 (FriendInvite). Form field: PlayerId (also accepts playerId/toPlayerId).
public class SendFriendRequestApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        if (accountId == 0) return @"{""success"":false,""error"":""auth required""}";

        int toId = 0;
        if (context.Request.HasFormContentType)
        {
            if (!int.TryParse(context.Request.Form["PlayerId"], out toId))
                if (!int.TryParse(context.Request.Form["playerId"], out toId))
                    int.TryParse(context.Request.Form["toPlayerId"], out toId);
        }
        if (toId == 0 && int.TryParse(context.Request.Query["PlayerId"], out var q))
            toId = q;
        if (toId == 0 || toId == accountId) return @"{""success"":false,""error"":""invalid target""}";

        SocialStore.SendFriendRequest(accountId, toId);
        return @"{""success"":true,""error"":""""}";
    }

    public string GetUrl() => "/api/relationships/v2/sendfriendrequest";
    public string GetMethod() => HttpMethods.Post;
}

// POST /api/relationships/v2/acceptfriendrequest — accepts a type-4 request and
// reaches the ORIGINAL requester with a type-40 (FriendRequestAccepted) message.
// Form field: PlayerId (also accepts playerId/fromPlayerId).
public class AcceptFriendRequestApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        if (accountId == 0) return @"{""success"":false,""error"":""auth required""}";

        int requesterId = 0;
        if (context.Request.HasFormContentType)
        {
            if (!int.TryParse(context.Request.Form["PlayerId"], out requesterId))
                if (!int.TryParse(context.Request.Form["playerId"], out requesterId))
                    int.TryParse(context.Request.Form["fromPlayerId"], out requesterId);
        }
        if (requesterId == 0 && int.TryParse(context.Request.Query["PlayerId"], out var q))
            requesterId = q;
        if (requesterId == 0 || requesterId == accountId) return @"{""success"":false,""error"":""invalid target""}";

        SocialStore.AcceptFriendRequest(accountId, requesterId);
        return @"{""success"":true,""error"":""""}";
    }

    public string GetUrl() => "/api/relationships/v2/acceptfriendrequest";
    public string GetMethod() => HttpMethods.Post;
}