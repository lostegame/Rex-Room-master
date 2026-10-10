namespace Rex_Room_20230414.API;

using Microsoft.AspNetCore.Http;

// GET /player/connection-info — the Photon credentials, current room, and
// experiment flags. Mirrors the reference match.app.ts handler: a
// { success, value, error } envelope. The value block is built ONCE by
// PhotonSessionInfo — the same shared source POST /player/heartbeat uses when a
// room is assigned — covering photonAuthToken (minted here for the caller), the
// three Photon app ids (our own, from PhotonCloudConfig), the pinned region,
// the caller's current Photon room, the assigned voice (Tachyon) server, and
// the verbatim experiments flags. Photon room id: the assigned room's id, else
// the requested roomInstanceId, else "" (empty string, never null — the
// client's decoder accepts a missing-value string over a null).
public class PlayerConnectionInfoApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context);
        if (accountId == null)
        {
            context.Response.StatusCode = 401;
            return "{\"error\":\"Unauthorized\"}";
        }

        return "{\"success\":true,\"value\":{" + PhotonSessionInfo.ValueJson(context, accountId.Value) + "},\"error\":null}";
    }

    public string GetUrl() => "/player/connection-info";
    public string GetMethod() => HttpMethods.Get;
}