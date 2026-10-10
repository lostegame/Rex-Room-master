namespace Rex_Room_20230414.API;
using System;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

// GET /rooms/{roomId}/subrooms/{subRoomId}/data
//
// Reference shape (rex room BepInEx LogOutput.log, 2023 build):
//   {"success":true,"error":null,"value":{
//      "room":{ ...full room DTO, SubRooms[].CurrentSave populated... },
//      "subRoomDataSave":{"subRoomDataSaveId":N,"subRoomId":N,"unityAssetId":null,
//        "unityAsset":null,"unityAssetHash":null,"dataBlob":"<date>/<guid>",
//        "dataBlobHash":null,"savedByAccountId":N,"savedOnPlatform":0,
//        "savedOnDeviceClass":0,"description":"","createdAt":"<iso>"}}}
//
// This used to be swallowed by the /rooms/{roomId} prefix handler and answered
// with a bare room DTO, so the client never received subRoomDataSave and sat on
// "Waiting for Keepsake/Instance Data download" until the room load timed out.
// Matching is longest-segment in Server.cs, so this 5-segment route outranks the
// 2-segment /rooms/{roomId}.
public class RoomSubRoomsDataApi : IApi
{
    private readonly string _url;

    public RoomSubRoomsDataApi(string url = "/rooms/{roomId}/subrooms/{subRoomId}/data")
    {
        _url = url;
    }

    public string Response(HttpContext context)
    {
        int accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        var (roomId, subRoomId) = IdsFromPath(context.Request.Path.Value ?? "");

        string roomJson;
        if (RoomCatalog.TryGetRoom(roomId, out var catalogRoom))
        {
            roomJson = RoomCatalog.RoomJson(catalogRoom, accountId);
        }
        else
        {
            // Unknown room: reuse the dynamic handler's shape rather than 404,
            // so a matchmade room the catalog does not seed still loads.
            roomJson = RoomJsonForUnknown(roomId, accountId);
        }

        string createdAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z";
        string blob = RoomData.RoomDataBlob;
        if (string.IsNullOrEmpty(blob))
        {
            blob = DateTime.UtcNow.ToString("yyyy-MM-dd") + "/" + Guid.NewGuid().ToString();
            RoomData.RoomDataBlob = blob;
        }

        string value =
            "{\"room\":" + roomJson +
            ",\"subRoomDataSave\":{" +
              $"\"subRoomDataSaveId\":{RoomData.SubRoomDataSaveId}," +
              $"\"subRoomId\":{subRoomId}," +
              "\"unityAssetId\":null,\"unityAsset\":null,\"unityAssetHash\":null," +
              $"\"dataBlob\":\"{blob}\",\"dataBlobHash\":null," +
              $"\"savedByAccountId\":{accountId},\"savedOnPlatform\":0,\"savedOnDeviceClass\":0," +
              $"\"description\":\"\",\"createdAt\":\"{createdAt}\"" +
            "}}";

        return "{\"success\":true,\"error\":null,\"value\":" + value + "}";
    }

    // Walks the path for the numeric segments after "rooms" and "subrooms", so the
    // same handler serves /rooms/x/subrooms/y/data and /roomserver/rooms/x/subrooms/y/data.
    private static (int roomId, int subRoomId) IdsFromPath(string path)
    {
        var segs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        int roomId = 0, subRoomId = 0;
        for (int i = 0; i < segs.Length - 1; i++)
        {
            if (string.Equals(segs[i], "rooms", StringComparison.OrdinalIgnoreCase))
                int.TryParse(segs[i + 1], out roomId);
            else if (string.Equals(segs[i], "subrooms", StringComparison.OrdinalIgnoreCase))
                int.TryParse(segs[i + 1], out subRoomId);
        }
        if (roomId == 0) roomId = RoomData.RoomId;
        if (subRoomId == 0) subRoomId = RoomData.SubRoomId;
        return (roomId, subRoomId);
    }

    private static string RoomJsonForUnknown(int roomId, int accountId)
    {
        // Build the same DTO from an empty catalog element so RoomJson fills in
        // every field with its documented default instead of us hand-rolling it.
        using var doc = JsonDocument.Parse("{}");
        return RoomCatalog.RoomJson(doc.RootElement.Clone(), accountId)
            .Replace("\"RoomId\":0", "\"RoomId\":" + roomId)
            .Replace("\"Name\":\"Room 0\"", "\"Name\":\"" + AccountHelper.DormName(accountId).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"")
            .Replace("\"ImageName\":\"\"", "\"ImageName\":\"DormRoom.jpg\"")
            .Replace("\"Description\":\"\"", "\"Description\":\"Your private room\"");
    }

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
}
