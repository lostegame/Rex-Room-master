namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

// /api/images/v5/cheered/bulk
//
// The client uses BOTH forms (rex room LogOutput.log):
//   GET  /api/images/v5/cheered/bulk?id=0
//        -> [{"SavedImageId":0,"IsCheered":false}]
//   POST /api/images/v5/cheered/bulk   body: id=15485&id=12898&id=6939...
//        -> [{"SavedImageId":15485,...},{"SavedImageId":12898,...}, ...]
//
// The POST form is a urlencoded body with one repeated `id` key and can carry ~120
// ids, so ids are read from the query string AND the form, and the reply mirrors
// every requested id in order - including id 0, the client's "no photo"
// placeholder. Nothing is cheered yet, so IsCheered is always false.
//
// Server.cs dispatches on a single GetMethod() per instance (first match wins),
// so this is registered once per method.
public class ImagesV5CheeredBulkApi : IApi
{
    private readonly string _url;
    private readonly string _method;

    public ImagesV5CheeredBulkApi(string url, string method)
    {
        _url = url;
        _method = method;
    }

    public string Response(HttpContext context)
    {
        var ids = ReadIds(context);

        var sb = new StringBuilder("[");
        for (int i = 0; i < ids.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append("{\"SavedImageId\":").Append(ids[i]).Append(",\"IsCheered\":false}");
        }
        // A request with no usable ids still answers with the placeholder entry
        // rather than an empty array the client cannot index.
        if (ids.Count == 0) sb.Append("{\"SavedImageId\":0,\"IsCheered\":false}");
        sb.Append(']');
        return sb.ToString();
    }

    private List<int> ReadIds(HttpContext context)
    {
        var ids = new List<int>();
        foreach (var raw in context.Request.Query["id"])
            if (int.TryParse(raw, out int q)) ids.Add(q);

        // Only read the body when it is the urlencoded form the POST path uses;
        // a JSON body would throw inside Form access.
        if (ids.Count == 0 && _method == HttpMethods.Post)
        {
            try
            {
                if (context.Request.HasFormContentType)
                    foreach (var raw in context.Request.Form["id"])
                        if (int.TryParse(raw, out int f)) ids.Add(f);
            }
            catch { }
        }
        return ids;
    }

    public string GetUrl() => _url;
    public string GetMethod() => _method;
}
