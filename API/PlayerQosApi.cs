namespace Rex_Room_20230414.API;

using Microsoft.AspNetCore.Http;

// GET /player/qos — the photon region probe targets, verbatim from the
// reference (Rec Room's own probe endpoints). The reported latency ranking is
// unused: every session is pinned to PhotonCloudConfig.Region.
public class PlayerQosApi : IApi
{
    public string Response(HttpContext context) => PhotonCloudConfig.QosRegionsJson();

    public string GetUrl() => "/player/qos";
    public string GetMethod() => HttpMethods.Get;
}