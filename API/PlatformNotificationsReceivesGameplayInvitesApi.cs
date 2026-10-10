namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class PlatformNotificationsReceivesGameplayInvitesApi : IApi
{
    public string Response(HttpContext context) => "{\"Success\":true,\"Valid\":true}";
    public string GetUrl() => "/pn/accounts/{accountId}/receives/GameplayInvites";
    public string GetMethod() => HttpMethods.Get;
}
