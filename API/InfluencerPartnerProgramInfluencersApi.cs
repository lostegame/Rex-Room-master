namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class InfluencerPartnerProgramInfluencersApi : IApi
{
    public string Response(HttpContext context) => "{\"InfluencerIds\":[]}";
    public string GetUrl() => "/api/influencerpartnerprogram/influencers";
    public string GetMethod() => HttpMethods.Get;
}
