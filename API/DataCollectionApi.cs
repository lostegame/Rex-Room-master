namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;

// POST /dc/data/heartbeat and /dc/data/event
//
// Data-collection (telemetry) ingest. The live client fires these constantly
// (heartbeat on a timer, event on every analytics action) - 34 hits during one
// short session. Neither had a handler, so every one fell through to the generic
// {"Success":true,"Valid":true} body.
//
// Both are fire-and-forget sinks: the client ignores the body and only cares that
// the request returns 2xx, otherwise it logs a telemetry failure and retries. An
// explicit empty ack is the right answer - nothing is stored.
//
// The generic fallback happened to be 200 too, so these were harmless in practice;
// they are implemented so the log stops reporting false fallbacks on the hot path.
public class DataCollectionApi : IApi
{
    private readonly string _url;

    public DataCollectionApi(string url)
    {
        _url = url;
    }

    public string Response(HttpContext context)
    {
        // Drain the body so the connection is reusable; the payload is discarded.
        try
        {
            if (context.Request.Body != null && context.Request.Body.CanRead)
            {
                context.Request.EnableBuffering();
                context.Request.Body.Position = 0;
                using var sr = new System.IO.StreamReader(context.Request.Body);
                sr.ReadToEnd();
            }
        }
        catch { }

        return "{\"Success\":true,\"Valid\":true}";
    }

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Post;
}
