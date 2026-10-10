namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class IncentivizedReferralsProgressApi : IApi
{
    public string Response(HttpContext context) => "{\"success\":true,\"value\":{\"ReferralsVerifiedCount\":0,\"PlayerReferralRewards\":[]}}";
    public string GetUrl() => "/api/incentivizedreferrals/progress";
    public string GetMethod() => HttpMethods.Get;
}
