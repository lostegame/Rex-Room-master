namespace Rex_Room_20230414.API;

using Microsoft.AspNetCore.Http;

public class ConfigV2Api : IApi
{
    public string Response(HttpContext context)
    {
        return @"{
""ShareBaseUrl"":""https://localhost:5229/{0}"",
""LevelProgressionMaps"":[{""Level"":1,""RequiredXp"":2,""GiftDropId"":1},{""Level"":2,""RequiredXp"":5,""GiftDropId"":1},{""Level"":3,""RequiredXp"":8,""GiftDropId"":1},{""Level"":4,""RequiredXp"":11,""GiftDropId"":1},{""Level"":5,""RequiredXp"":14,""GiftDropId"":1},{""Level"":6,""RequiredXp"":17,""GiftDropId"":1},{""Level"":7,""RequiredXp"":20,""GiftDropId"":1},{""Level"":8,""RequiredXp"":23,""GiftDropId"":1},{""Level"":9,""RequiredXp"":26,""GiftDropId"":1},{""Level"":10,""RequiredXp"":29,""GiftDropId"":1},{""Level"":11,""RequiredXp"":32,""GiftDropId"":1},{""Level"":12,""RequiredXp"":35,""GiftDropId"":1},{""Level"":13,""RequiredXp"":38,""GiftDropId"":1},{""Level"":14,""RequiredXp"":41,""GiftDropId"":1},{""Level"":15,""RequiredXp"":46,""GiftDropId"":1},{""Level"":16,""RequiredXp"":51,""GiftDropId"":1},{""Level"":17,""RequiredXp"":56,""GiftDropId"":1},{""Level"":18,""RequiredXp"":61,""GiftDropId"":1},{""Level"":19,""RequiredXp"":66,""GiftDropId"":1},{""Level"":20,""RequiredXp"":71,""GiftDropId"":1},{""Level"":21,""RequiredXp"":76,""GiftDropId"":1},{""Level"":22,""RequiredXp"":81,""GiftDropId"":1},{""Level"":23,""RequiredXp"":86,""GiftDropId"":1},{""Level"":24,""RequiredXp"":91,""GiftDropId"":1},{""Level"":25,""RequiredXp"":96,""GiftDropId"":1},{""Level"":26,""RequiredXp"":101,""GiftDropId"":1},{""Level"":27,""RequiredXp"":106,""GiftDropId"":1},{""Level"":28,""RequiredXp"":111,""GiftDropId"":1},{""Level"":29,""RequiredXp"":116,""GiftDropId"":1},{""Level"":30,""RequiredXp"":126,""GiftDropId"":1},{""Level"":31,""RequiredXp"":136,""GiftDropId"":1},{""Level"":32,""RequiredXp"":146,""GiftDropId"":1},{""Level"":33,""RequiredXp"":156,""GiftDropId"":1},{""Level"":34,""RequiredXp"":166,""GiftDropId"":1},{""Level"":35,""RequiredXp"":176,""GiftDropId"":1},{""Level"":36,""RequiredXp"":186,""GiftDropId"":1},{""Level"":37,""RequiredXp"":196,""GiftDropId"":1},{""Level"":38,""RequiredXp"":206,""GiftDropId"":1},{""Level"":39,""RequiredXp"":216,""GiftDropId"":1},{""Level"":40,""RequiredXp"":226,""GiftDropId"":1},{""Level"":41,""RequiredXp"":236,""GiftDropId"":1},{""Level"":42,""RequiredXp"":246,""GiftDropId"":1},{""Level"":43,""RequiredXp"":256,""GiftDropId"":1},{""Level"":44,""RequiredXp"":266,""GiftDropId"":1},{""Level"":45,""RequiredXp"":276,""GiftDropId"":1},{""Level"":46,""RequiredXp"":286,""GiftDropId"":1},{""Level"":47,""RequiredXp"":296,""GiftDropId"":1},{""Level"":48,""RequiredXp"":306,""GiftDropId"":1},{""Level"":49,""RequiredXp"":316,""GiftDropId"":1},{""Level"":50,""RequiredXp"":316,""GiftDropId"":1},{""Level"":51,""RequiredXp"":316,""GiftDropId"":1}],
""DailyObjectives"":[[{""type"":1000,""score"":2,""xp"":10},{""type"":501,""score"":2,""xp"":10},{""type"":302,""score"":2,""xp"":10}],[{""type"":7,""score"":1,""xp"":10},{""type"":1033,""score"":2,""xp"":10},{""type"":2003,""score"":2,""xp"":10}],[{""type"":1030,""score"":1,""xp"":10},{""type"":1000,""score"":2,""xp"":10},{""type"":4001,""score"":1,""xp"":10}],[{""type"":902,""score"":1,""xp"":10},{""type"":1032,""score"":2,""xp"":10},{""type"":1022,""score"":2,""xp"":10}],[{""type"":4001,""score"":2,""xp"":10},{""type"":6,""score"":1,""xp"":10},{""type"":101,""score"":1,""xp"":10}],[{""type"":1031,""score"":2,""xp"":10},{""type"":1022,""score"":2,""xp"":10},{""type"":1051,""score"":1,""xp"":10}],[{""type"":1010,""score"":1,""xp"":10},{""type"":802,""score"":1,""xp"":10},{""type"":2004,""score"":2,""xp"":10}]],
""ServerMaintenance"":{""StartsInMinutes"":0},
""AutoMicMutingConfig"":{""MicSpamVolumeThreshold"":1.125,""MicVolumeSampleInterval"":0.25,""MicVolumeSampleRollingWindowLength"":7,""MicSpamSamplePercentageForWarning"":0.8,""MicSpamSamplePercentageForWarningToEnd"":0.2,""MicSpamSamplePercentageForForceMute"":0.8,""MicSpamSamplePercentageForForceMuteToEnd"":0.2,""MicSpamWarningStateVolumeMultiplier"":0.25},
""StorefrontConfig"":{""MinPlayerLevelForGifting"":1},
""RoomKeyConfig"":{""MaxKeysPerRoom"":100},
""RoomCurrencyConfig"":{""AwardCurrencyCooldownSeconds"":10}
}";
    }

    public string GetUrl()
    {
        return "/api/config/v2";
    }

    public string GetMethod()
    {
        return HttpMethods.Get;
    }
}
