namespace Rex_Room_20230414.API;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Rex_Room_20230414.Database;

// Friendship + message store, persisted per account under Player\{id}\.
// Mirrors the reference friend contract: a friend request is SHOWN to the
// target as a Message of type 4 (FriendInvite) via
// /api/relationships/v2/sendfriendrequest; an acceptance reaches the requester
// as type 40 (FriendRequestAccepted) via acceptfriendrequest (and the
// crossing-request auto-accept). Relationship DTO is
// { Id, PlayerID, RelationshipType, Favorited, Muted, Ignored }, type 3 = friends.
public static class SocialStore
{
    private static readonly object _lock = new();

    // ---- relationships -----------------------------------------------------

    public static List<RelationshipEntry> GetRelationships(int accountId)
    {
        string raw = Load(accountId, "relationships");
        // NO SEEDING. This used to invent friendships with accounts 1 and 2 (the
        // RRO creators) for every account that had never received a friend request,
        // purely to "make the social panel look alive". That made unrelated players
        // show up as friends automatically, with no request and no acceptance.
        // An account with no relationships file has NO friends - relationships are
        // only ever created by AcceptFriendRequest (a real request + acceptance).
        if (string.IsNullOrEmpty(raw))
            return new List<RelationshipEntry>();
        try
        {
            var list = JsonSerializer.Deserialize<List<RelationshipEntry>>(raw);
            return list ?? new List<RelationshipEntry>();
        }
        catch { return new List<RelationshipEntry>(); }
    }

    private static string Load(int accountId, string key) =>
        DatabaseManager.LoadAsync(accountId.ToString(), "social." + key).GetAwaiter().GetResult();

    private static void Save(int accountId, string key, object value) =>
        DatabaseManager.SaveAsync(accountId.ToString(), "social." + key,
            JsonSerializer.Serialize(value)).GetAwaiter().GetResult();

    public static bool AreFriends(int a, int b)
    {
        if (a == b) return true;
        return GetRelationships(a).Any(r => r.PlayerID == b && r.RelationshipType == 3);
    }

    public static string FriendsJson(int accountId)
    {
        var rels = GetRelationships(accountId).OrderByDescending(r => r.Id).ToList();
        var sb = new System.Text.StringBuilder();
        sb.Append('[');
        for (int i = 0; i < rels.Count; i++)
        {
            if (i > 0) sb.Append(',');
            var r = rels[i];
            sb.Append($@"{{""Id"":{r.Id},""PlayerID"":{r.PlayerID},""RelationshipType"":{r.RelationshipType},"
                    + $@"""Favorited"":{r.Favorited},""Muted"":{r.Muted},""Ignored"":{r.Ignored}}}");
        }
        sb.Append(']');
        return sb.ToString();
    }

    // ---- messages ----------------------------------------------------------

    public static List<MessageEntry> GetMessages(int accountId)
    {
        string raw = Load(accountId, "messages");
        if (string.IsNullOrEmpty(raw)) return new List<MessageEntry>();
        try
        {
            var list = JsonSerializer.Deserialize<List<MessageEntry>>(raw);
            return list ?? new List<MessageEntry>();
        }
        catch { return new List<MessageEntry>(); }
    }

    private static void AppendMessage(MessageEntry msg)
    {
        lock (_lock)
        {
            var inbox = GetMessages(msg.ToPlayerId);
            inbox.Add(msg);
            Save(msg.ToPlayerId, "messages", inbox.OrderByDescending(m => m.Id).ToList());
        }
    }

    public static string MessagesJson(int accountId)
    {
        // The inbox: messages addressed TO this account, freshest first.
        // The game client fetches `GET /api/messages/v2/get` on Dorm/login and
        // parses this body as a TOP-LEVEL JSON ARRAY (decompiled parser error:
        // "expected:'[', actual:'{', at offset:0"); the live RecRoom reference
        // server replies with a bare array (`body=[]` when the inbox is empty).
        // Element shape mirrors the real `/api/messages/v3/get` message object.
        var inbox = GetMessages(accountId).OrderByDescending(m => m.Id).ToList();
        var sb = new System.Text.StringBuilder();
        sb.Append('[');
        for (int i = 0; i < inbox.Count; i++)
        {
            if (i > 0) sb.Append(',');
            var m = inbox[i];
            sb.Append($@"{{""Id"":{m.Id},""FromPlayerId"":{m.FromPlayerId},"
                    + $@"""SentTime"":""{m.SentTime}"",""Type"":{m.Type},""Data"":""{Esc(m.Data)}"",""RoomId"":null,""PlayerEventId"":null}}");
        }
        sb.Append(']');
        return sb.ToString();
    }

    // ---- friend request flow ----------------------------------------------

    public static void SendFriendRequest(int fromId, int toId)
    {
        var msg = new MessageEntry
        {
            Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            FromPlayerId = fromId,
            ToPlayerId = toId,
            SentTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
            Type = 4, // FriendInvite
            Data = ""
        };
        // Ignore the empty crossing guard: a request that already exists just
        // re-delivers the same type-4 message.
        AppendMessage(msg);
    }

    public static void AcceptFriendRequest(int accepterId, int requesterId)
    {
        lock (_lock)
        {
            // Relationship on both sides (type 3 = friends).
            AddRelationship(accepterId, requesterId);
            AddRelationship(requesterId, accepterId);

            // Acceptance message (type 40) reaches the ORIGINAL requester.
            var msg = new MessageEntry
            {
                Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                FromPlayerId = accepterId,
                ToPlayerId = requesterId,
                SentTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                Type = 40, // FriendRequestAccepted
                Data = ""
            };
            AppendMessage(msg);
        }
    }

    private static void AddRelationship(int accountId, int otherId)
    {
        var rels = GetRelationships(accountId);
        bool has = rels.Any(r => r.PlayerID == otherId && r.RelationshipType == 3);
        if (!has)
        {
            rels.Add(new RelationshipEntry { Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), PlayerID = otherId, RelationshipType = 3 });
            Save(accountId, "relationships", rels);
        }
    }

    private static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

public class RelationshipEntry
{
    public long Id { get; set; }
    public int PlayerID { get; set; }
    public int RelationshipType { get; set; }
    public int Favorited { get; set; }
    public int Muted { get; set; }
    public int Ignored { get; set; }
}

public class MessageEntry
{
    public long Id { get; set; }
    public int FromPlayerId { get; set; }
    public int ToPlayerId { get; set; }
    public string SentTime { get; set; } = "";
    public int Type { get; set; }
    public string Data { get; set; } = "";
}