namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;

// GET /api/playerevents/v1/search and /searchlive
//
// The Watch tab. Both share one handler; searchlive filters to State==1. These were
// falling through to the generic 200 {"Success":true}, which is an object where the
// client expects a list, so the tab rendered empty.
public class PlayerEventsSearchApi : IApi
{
    private readonly string _url;
    private readonly bool _liveOnly;

    public PlayerEventsSearchApi(string url, bool liveOnly)
    {
        _url = url;
        _liveOnly = liveOnly;
    }

    public string Response(HttpContext context)
    {
        int skip = 0, take = 20;
        int.TryParse(context.Request.Query["skip"], out skip);
        int.TryParse(context.Request.Query["take"], out take);

        // The client passes the search box under a few different keys.
        var query = context.Request.Query["query"].ToString();
        if (string.IsNullOrEmpty(query)) query = context.Request.Query["search"].ToString();
        if (string.IsNullOrEmpty(query)) query = context.Request.Query["q"].ToString();
        if (string.IsNullOrEmpty(query)) query = context.Request.Query["name"].ToString();

        var items = PlayerEventCatalog.Search(query, _liveOnly, skip, take);

        var sb = new StringBuilder("[");
        for (int i = 0; i < items.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(PlayerEventCatalog.Dto(items[i]));
        }
        sb.Append(']');
        return sb.ToString();
    }

    public string GetUrl() => _url;
    public string GetMethod() => HttpMethods.Get;
}
