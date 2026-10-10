namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;

// The last routes the live client still hit that answered with the generic
// {"Success":true,"Valid":true} object instead of the shape they bind to. The
// reference server never exercises these, so each shape is built to match the
// sibling endpoint in the same family:
//
//   GET  /api/images/v1/slideshow                 -> list of slideshow DTOs
//          (ImagesSlideshowApi was registered at /api/images/slideshow, missing
//           the "v1" segment, so the versioned path never matched)
//   POST /api/objectives/v1/cleargroup            -> {"Results":[],"TotalResults":0}
//   POST /api/playerevents/v2                     -> created/updated event wrapper
//   GET  /api/playerevents/v1/{eventId}           -> single event DTO (404 unknown)
//   GET  /api/playerevents/v1/{eventId}/responses -> {"Created":[],"Responses":[]}
//   POST /api/customAvatarItems/v1                -> create/publish result
//   GET  /api/customAvatarItems/v1/design         -> design-tool metadata
//   PUT  /api/customAvatarItems/v1/design         -> save a work-in-progress design
//
// Empty collections are the meaningful answers here: they tell the client
// "nothing to show" instead of handing it an object it cannot deserialize, which
// is what leaves a pane permanently blank or spinners the UI.
public class RemainingShapesApi : IApi
{
    private readonly string _url;
    private readonly string _kind;

    public RemainingShapesApi(string url, string kind)
    {
        _url = url;
        _kind = kind;
    }

    public string Response(HttpContext context)
    {
        switch (_kind)
        {
            case "slideshow":
                // A bare list, matching how the client reads its slideshows.
                return "[]";

            case "cleargroup":
                return "{\"Results\":[],\"TotalResults\":0}";

            case "playerevents-create":
                // Create/update returns the event envelope, mirroring
                // /api/playerevents/v1/all -> {"Created":[],"Responses":[]}.
                return "{\"Created\":[],\"Responses\":[]}";

            case "responses":
                return "{\"Created\":[],\"Responses\":[]}";

            case "event-detail":
                // The id arrives as a PATH segment (/api/playerevents/v1/112), not a
                // query parameter, so parse it out of the path.
                int eventId = 0;
                var m = System.Text.RegularExpressions.Regex.Match(
                    context.Request.Path.Value ?? "", @"/playerevents/v1/(\d+)");
                if (m.Success) int.TryParse(m.Groups[1].Value, out eventId);
                else int.TryParse(context.Request.Query["eventId"], out eventId);
                var ev = PlayerEventCatalog.Find(eventId);
                // Assign unconditionally: this handler instance is shared across
                // requests, so only ever setting it to true would make the 404
                // stick for every later event id.
                _notFound = ev == null;
                if (ev == null)
                    return "{\"success\":false,\"error\":{\"message\":\"not found\"}}";
                return PlayerEventCatalog.Dto(ev);

            case "cai-create":
                // Nothing is actually published; the client only needs a success
                // envelope with an empty payload list.
                return "{\"Success\":true,\"Valid\":true,\"Data\":[]}";

            case "cai-design-save":
                // PUT /api/customAvatarItems/v1/design is the design tool saving
                // a work-in-progress. Same accept-and-ignore envelope as
                // POST /api/customAvatarItems/v1, which is what the client treats
                // as "saved" - returning the metadata shape here (what GET uses)
                // would not deserialize into the save result.
                return "{\"Success\":true,\"Valid\":true,\"Data\":[]}";

            default:
                // design-tool metadata
                return "{\"BaseAvatarItems\":[],\"Colors\":[],\"DefaultColor\":\"\"}";
        }
    }

    private bool _notFound = false;

    public string GetUrl() => _url;
    public string GetMethod() => _kind switch
    {
        "cleargroup" => HttpMethods.Post,
        "playerevents-create" => HttpMethods.Post,
        "cai-create" => HttpMethods.Post,
        "cai-design-save" => HttpMethods.Put,
        _ => HttpMethods.Get,
    };
    public int GetStatusCode() => _notFound ? 404 : 200;
}
