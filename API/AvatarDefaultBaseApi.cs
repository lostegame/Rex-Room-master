namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class AvatarDefaultBaseApi : IApi
{
    public string Response(HttpContext context) =>
        @"[
  {""AvatarItemDesc"":""c5d70cb4-71dd-4fe4-b719-34fe2073c611,"",""AvatarItemType"":0,""PlatformMask"":-1,""FriendlyName"":""(UGCTee_Shirt) "",""Tooltip"":"""",""Rarity"":-1,""TagList"":"""",""AvatarItemId"":2184,""IsBaseAvatarItem"":true,""CreatedAt"":""2022-04-19T23:40:30.807Z"",""ThumbnailImage"":""KXfytDhXzES2yco-rwqDSA.png""},
  {""AvatarItemDesc"":""95a519de-f2cb-429c-b014-508477f20d42,"",""AvatarItemType"":0,""PlatformMask"":-1,""FriendlyName"":""(UGCPulloverHoodie_Shirt) "",""Tooltip"":"""",""Rarity"":-1,""TagList"":"""",""AvatarItemId"":2918,""IsBaseAvatarItem"":true,""CreatedAt"":""2023-04-07T17:07:07.04Z"",""ThumbnailImage"":""m4UIuZjNzEWsCP1gpZBgjg.png""}
]";
    public string GetUrl() => "/api/avatar/v1/defaultbaseavataritems";
    public string GetMethod() => HttpMethods.Get;
}