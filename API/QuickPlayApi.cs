namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class QuickPlayApi : IApi
{
    public string Response(HttpContext context)
    {
        RoomData.AssignRoom(context);
        return $@"{{""errorCode"":0,""result"":0,""roomInstance"":{RoomData.RoomJson(context)},""correlationId"":""00000000-0000-0000-0000-000000000000""}}";
    }

    public string GetUrl() => "/quickplay";
    public string GetMethod() => HttpMethods.Post;
}
