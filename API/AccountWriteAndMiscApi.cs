namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using Rex_Room_20230414.Database;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;

// The last four routes the live client still answered with the generic
// {"Success":true,"Valid":true} object (found by replaying every path in the
// client's own log against the server - 299 paths, this was all that remained).
//
//   PUT /account/me/bio              -> {"bio":"..."}           (GET counterpart exists)
//   PUT /account/me/personalpronouns -> {"personalPronouns":N}
//   POST /purchase/v1/cleanuppending -> {"CleanedUp":false}
//   GET  /roomserver/playlists       -> list of playlist DTOs
//
// The two account writes really do persist, so the value the player types into
// the profile shows up on the next /account/me read instead of silently vanishing.
public class AccountWriteAndMiscApi : IApi
{
    private readonly string _url;
    private readonly string _kind;

    public AccountWriteAndMiscApi(string url, string kind)
    {
        _url = url;
        _kind = kind;
    }

    public string Response(HttpContext context)
    {
        switch (_kind)
        {
            case "bio":
            {
                int accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
                // The client sends "bio=<text>" (form-urlencoded, often with no
                // Content-Type header). Storing the body verbatim persisted the
                // literal "bio=fffff", which the profile then rendered as
                // "bio=fffff".
                string bio = AccountHelper.FormValue(context, "bio");
                if (accountId != 0)
                {
                    try
                    {
                        // Load the whole account, mutate the one field, save it back.
                        // SaveAccount(Account) is the account-aware helper; calling
                        // DatabaseManager.SavePlayerData directly did not stick (the
                        // read-back returned the old value).
                        var acc = AccountHelper.GetAccountById(accountId);
                        if (acc != null)
                        {
                            acc.Bio = bio;
                            AccountHelper.SaveAccount(acc);
                        }
                    }
                    catch { }
                }
                return "{\"bio\":" + JsonSerializer.Serialize(bio) + "}";
            }

            case "pronouns":
            {
                int accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
                int pronouns = 0;
                var raw = AccountHelper.FormValue(context, "personalPronouns", "pronouns");
                if (!int.TryParse(raw, out pronouns))
                {
                    // Some builds post a bare number, or a JSON object.
                    var body = AccountHelper.RawBody(context).Trim();
                    if (body.StartsWith("{")) body = AccountHelper.FormValue(context, "value");
                    if (!int.TryParse(body.Trim('"'), out pronouns)) pronouns = 0;
                }
                if (accountId != 0)
                {
                    try
                    {
                        var acc = AccountHelper.GetAccountById(accountId);
                        if (acc != null)
                        {
                            acc.PersonalPronouns = pronouns;
                            AccountHelper.SaveAccount(acc);
                        }
                    }
                    catch { }
                }
                return "{\"personalPronouns\":" + pronouns + "}";
            }

            case "cleanup":
                // Nothing is ever left pending locally, so there is nothing to clean.
                return "{\"CleanedUp\":false}";

            default:
                // Playlists: a bare list, same shape as the other room browse lists.
                return "[]";
        }
    }

    // Retained for callers that just want the raw body unwrapped from a JSON
    // string literal; new code should prefer AccountHelper.FormValue, which also
    // handles the form-urlencoded "key=value" bodies these writes actually use.
    private static string BodyString(HttpContext context)
    {
        var raw = AccountHelper.RawBody(context).Trim();
        if (raw.Length == 0) return context.Request.Query["value"].ToString();

        if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
        {
            try { return JsonSerializer.Deserialize<string>(raw) ?? ""; }
            catch { return raw.Trim('"'); }
        }
        return raw;
    }

    public string GetUrl() => _url;
    public string GetMethod() => _kind is "bio" or "pronouns"
        ? HttpMethods.Put
        : _kind == "cleanup" ? HttpMethods.Post : HttpMethods.Get;
}
