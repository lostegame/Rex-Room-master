namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ObjectivesMyProgressApi : IApi
{
    public string Response(HttpContext context) =>
        @"{
  ""Objectives"":[{""Index"":2,""Group"":0,""Progress"":0,""VisualProgress"":0,""IsCompleted"":false,""HasClaimedReward"":false}],
  ""ObjectiveGroups"":[{""Group"":0,""IsCompleted"":false,""ClearedAt"":""2026-03-31T01:26:32.5904766""}]
}";

    public string GetUrl() => "/api/objectives/v1/myprogress";
    public string GetMethod() => HttpMethods.Get;
}
