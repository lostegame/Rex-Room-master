namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ChallengeCurrentApi : IApi
{
    public string Response(HttpContext context) =>
        @"[
  {""Order"":0,""Objective"":38,""Count"":1,""CreditAmount"":25},
  {""Order"":1,""Objective"":32,""Count"":1,""CreditAmount"":25},
  {""Order"":2,""Objective"":2,""Count"":1,""CreditAmount"":25},
  {""Order"":3,""Objective"":30,""Count"":1,""CreditAmount"":25},
  {""Order"":4,""Objective"":6,""Count"":1,""CreditAmount"":25}
]";

    public string GetUrl() => "/api/checklist/v1/current";
    public string GetMethod() => HttpMethods.Get;
}
