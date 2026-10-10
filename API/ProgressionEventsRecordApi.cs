namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class ProgressionEventsRecordApi : IApi
{
    public string Response(HttpContext context) => "{\"success\":false,\"error\":{\"message\":\"not found\"}}";
    public string GetUrl() => "/api/progressionEvents/record/{eventId}";
    public string GetMethod() => HttpMethods.Get;
    public int GetStatusCode() => 404;
}