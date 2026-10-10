namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using Rex_Room_20230414.Database;

public class InfluencerPartnerProgramInfluencerApi : IApi
{
    public string Response(HttpContext context) => "0";
    public string GetUrl() => "/api/influencerpartnerprogram/influencer";
    public string GetMethod() => HttpMethods.Get;
}
