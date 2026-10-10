namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

// POST /api/PlayerReporting/v1/hile — anti-cheat/crash/telemetry report. Reference #66: RESP BODY true/false (scalar boolean).
// Must return a scalar bool (the client parses it as boolean), NOT a JSON object.
// The request body is the CLIENT'S error report (e.g. "Message=Dowload failed: An error occurred:&Type=311"
// in the successful reference run). The reference server read it; prOnes server must too, so the
// client-side failure reason is preserved in the request log instead of being discarded.
public class PlayerReportingHileApi : IApi
{
    public string Response(HttpContext context)
    {
        var body = "";
        try
        {
            context.Request.EnableBuffering();
            using (var reader = new StreamReader(context.Request.Body, System.Text.Encoding.UTF8, false, 8192, leaveOpen: true))
            {
                body = reader.ReadToEndAsync().GetAwaiter().GetResult();
            }
            context.Request.Body.Position = 0;
        }
        catch (Exception ex)
        {
            body = "(read-failed: " + ex.Message + ")";
        }

        try
        {
            System.IO.File.AppendAllText(@"C:\Users\LOSTEG~1\AppData\Local\Temp\opencode\rexroom_requests.log",
                $"{DateTime.Now:HH:mm:ss.fff} [HILE-BODY] {body}" + Environment.NewLine);
        }
        catch { }
        Console.WriteLine($"[HILE-BODY] {body}");

        // The reference RecFlare server always answered FALSE for hile and the client continued
        // living; when asked with the SAME report ([0:winhttp.dll]&Type=5) the client aborts and
        // logs out if the server says "true". Return false to signal "non-fatal, keep going".
        return "false";
    }
    public string GetUrl() => "/api/PlayerReporting/v1/hile";
    public string GetMethod() => HttpMethods.Post;
}