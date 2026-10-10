namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class PurchaseCampaignAllCurrentV2Api : IApi
{
    public string Response(HttpContext context) => "[]";
    public string GetUrl() => "/purchasecampaign/allcurrent/v2";
    public string GetMethod() => HttpMethods.Get;
}
