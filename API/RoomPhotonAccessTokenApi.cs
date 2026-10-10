namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using Rex_Room_20230414.Database;

public class RoomPhotonAccessTokenApi : IApi
{
    private readonly string _url;

    public RoomPhotonAccessTokenApi(string url = "/photon_access_token")
    {
        _url = url;
    }

    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context) ?? 2056;

        string token = "";
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            token = authHeader.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrEmpty(token))
            token = AccountHelper.GetAccountById(accountId)?.Token ?? "";

        var perms = new System.Collections.Generic.List<string>();
        void Add(string permission, int role, bool overrideFlag, string value = "True")
        {
            perms.Add($"{{\"Override\":{(overrideFlag ? "true" : "false")},\"Permission\":\"{permission}\",\"Role\":{role},\"Type\":0,\"Value\":\"{value}\"}}");
        }

        // CREATION RIGHTS ARE DORM-ONLY.
        //
        // CAN_USE_MAKER_PEN (the creation pen) used to be granted unconditionally
        // in every room, so players could create/modify content in the PUBLIC
        // rooms. It must only be true while the player stands in their OWN Dorm.
        // The reference capture shows Value "True" for every permission, but that
        // capture was taken in a Dorm (RoomInstanceId 1194970), so it documents the
        // in-dorm grant, not the public-room case; there is no public-room capture
        // to copy. "Value":"False" on the Maker Pen entry is the only denial
        // signal this payload has, so that is what public rooms emit.
        //
        // Everything else is left exactly as the reference has it: spawning
        // inventions/objects still works everywhere, which the Gift Box spawn flow
        // depends on.
        bool inOwnDorm = RoomData.IsInDorm(context);
        string makerPen = inOwnDorm ? "True" : "False";

        // Matches the reference captures verbatim (11 entries, exact order).
        Add("CAN_USE_ROOM_RESET_BUTTON", 0, true);
        Add("CAN_USE_DELETE_ALL_BUTTON", 0, true);
        Add("CAN_SAVE_INVENTIONS", 0, true);
        Add("CAN_SPAWN_INVENTIONS", 0, true);
        Add("CAN_USE_PLAY_GIZMOS_TOGGLE", 0, true);
        Add("CAN_USE_MAKER_PEN", 30, false, makerPen);
        Add("CAN_USE_ROOM_RESET_BUTTON", 30, true);
        Add("CAN_USE_DELETE_ALL_BUTTON", 30, true);
        Add("CAN_SAVE_INVENTIONS", 30, true);
        Add("CAN_SPAWN_INVENTIONS", 30, true);
        Add("CAN_USE_PLAY_GIZMOS_TOGGLE", 30, true);

        // The Photon access token must carry the instance the caller is ACTUALLY
        // in, not the static dorm constant: after /matchmake/room/{id} the player
        // holds a different roomInstanceId, and advertising the dorm here makes
        // the client join Photon under the wrong instance - it can then never
        // finish joining, which is what left the player stuck when returning to
        // the dorm. Fall back to the CALLER'S OWN dorm only when nothing is
        // assigned - the shared global dorm id would point a second player at
        // the first player's Photon instance.
        var roomInstanceId = RoomData.TryGetAssignedInstanceId(context, out var assignedId)
            ? assignedId
            : RoomData.DormInstanceId(AccountHelper.GetAccountIdFromToken(context) ?? 0);

        return $@"{{
  ""Permissions"":[{string.Join(",", perms)}],
  ""PhotonAccessToken"":""{token.Replace("\"", "'")}"",
  ""RoomInstanceId"":{roomInstanceId}
}}";
    }

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
}