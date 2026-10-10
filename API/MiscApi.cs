namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;

// The remaining routes the client calls that were falling through to the generic
// 200 {"Success":true}. Each response below is copied from the reference's own body
// (rex room LogOutput.log, 2023 build):
//
//   GET  /showcase/{accountId}                 -> []
//   GET  /reminder/currentTokenBundles/v2      -> []
//   POST /api/checklist/v1/complete            -> {"BalanceUpdates":[{"UpdateResponse":303,"Data":[]}],
//                                                 "Balance":0,"CurrencyType":2,"BalanceType":-2}
//   POST /api/consumables/v1/consume            -> {"error":"","success":true,"value":null}
//   POST /api/sanitize/v1                       -> the submitted text, JSON-quoted
//                                                 (the reference echoes "asdfsdf")
//   GET  /actionlink                            -> 403 (the reference answers 403 too)
//
// /api/sanitize/v1 is a text echo used by the chat/box filter, so it must read the
// request body and return only the text, quoted, rather than a canned string.
public class MiscApi : IApi
{
    private readonly string _url;
    private readonly string _kind;
    private int _status = 200;

    public MiscApi(string url, string kind)
    {
        _url = url;
        _kind = kind;
    }

    public string Response(HttpContext context)
    {
        _status = 200;
        switch (_kind)
        {
            case "checklist":
                return "{\"BalanceUpdates\":[{\"UpdateResponse\":303,\"Data\":[]}]," +
                       "\"Balance\":0,\"CurrencyType\":2,\"BalanceType\":-2}";

            case "consume":
                return "{\"error\":\"\",\"success\":true,\"value\":null}";

            case "sanitize":
                return "\"" + JsonString(SanitizeText(context)) + "\"";

            case "actionlink":
                // Match the reference: action links are not served.
                _status = 403;
                return "{\"success\":false,\"error\":{\"message\":\"forbidden\"}}";

            default:
                // showcase/{id} and reminder/currentTokenBundles/v2
                return "[]";
        }
    }

    // The chat filter posts the WHOLE message envelope, not just the text:
    //   {"Value":"hello","ReplacementChar":42,"Context":"RoomChat","Intent":1,"ruleset":0}
    // The client then broadcasts whatever comes back as the chat line, so echoing
    // the envelope verbatim made every message render as that raw JSON instead of
    // the words. Unwrap it and answer with the text alone.
    // Older builds posted a bare JSON string ("asdfsdf"), so that form is unwrapped
    // too, and anything unparseable is treated as literal text.
    private static string SanitizeText(HttpContext context)
    {
        var raw = (ReadBody(context) ?? "").Trim();
        if (raw.Length == 0) return "";
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.ValueKind == System.Text.Json.JsonValueKind.Object &&
                root.TryGetProperty("Value", out var v))
                return v.ValueKind == System.Text.Json.JsonValueKind.String
                    ? (v.GetString() ?? "")
                    : v.ToString();
            if (root.ValueKind == System.Text.Json.JsonValueKind.String)
                return root.GetString() ?? "";
        }
        catch { }
        return raw;
    }

    private static string ReadBody(HttpContext context)
    {
        try
        {
            using var sr = new System.IO.StreamReader(context.Request.Body);
            return sr.ReadToEnd();
        }
        catch { return ""; }
    }

    // Escape for a JSON string literal; the reference echoes the raw text.
    private static string JsonString(string s) =>
        s.Replace("\\", "\\\\")
         .Replace("\"", "\\\"")
         .Replace("\r", "")
         .Replace("\n", "\\n")
         .Replace("\t", "\\t");

    public string GetUrl() => _url;
    public string GetMethod() => _kind switch
    {
        "checklist" => HttpMethods.Post,
        "consume" => HttpMethods.Post,
        "sanitize" => HttpMethods.Post,
        _ => HttpMethods.Get,
    };
    public int GetStatusCode() => _status;
}
