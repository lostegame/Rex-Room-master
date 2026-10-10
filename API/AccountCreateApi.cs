namespace Rex_Room_20230414.API;

using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Rex_Room_20230414.Database;

public class AccountCreateApi : IApi
{
    private static readonly object _lock = new object();

    public const string DefaultProfileImage = "DefaultProfileImage.jpg";

    public string Response(HttpContext context)
    {
        try
        {
            string username = "";
            string password = "";
            string platformId = "";
            string displayName = "";
            string birthday = "";

            if (context.Request.HasFormContentType)
            {
                var form = context.Request.Form;
                username = form.ContainsKey("username") ? form["username"].ToString() : "";
                password = form.ContainsKey("password") ? form["password"].ToString() : "";
                platformId = form.ContainsKey("platformId") ? form["platformId"].ToString() : "";
                displayName = form.ContainsKey("displayName") ? form["displayName"].ToString() : "";
                birthday = form.ContainsKey("birthday") ? form["birthday"].ToString() : "";
                if (string.IsNullOrEmpty(username)) username = displayName;
                if (string.IsNullOrEmpty(username)) username = form.ContainsKey("name") ? form["name"].ToString() : "";
                Console.WriteLine($"[DEBUG /account/create] Form: username='{username}' password='{password}' platformId='{platformId}' displayName='{displayName}' birthday='{birthday}'");
                foreach (var key in form.Keys) Console.WriteLine($"[DEBUG /account/create] Form field: {key} = '{form[key]}'");
            }
            else if (context.Request.ContentType != null && context.Request.ContentType.Contains("json"))
            {
                string body = "";
                if (context.Request.Body != null)
                {
                    using var reader = new StreamReader(context.Request.Body);
                    body = reader.ReadToEndAsync().Result ?? "";
                }
                Console.WriteLine($"[DEBUG /account/create] JSON body: '{body}'");

                if (!string.IsNullOrEmpty(body))
                {
                    try
                    {
                        var json = JsonDocument.Parse(body);
                        var root = json.RootElement;
                        if (root.TryGetProperty("username", out var u)) username = u.GetString() ?? "";
                        if (root.TryGetProperty("displayName", out var dn)) { displayName = dn.GetString() ?? ""; if (string.IsNullOrEmpty(username)) username = displayName; }
                        if (root.TryGetProperty("name", out var n)) { if (string.IsNullOrEmpty(username)) username = n.GetString() ?? ""; }
                        if (root.TryGetProperty("password", out var p)) password = p.GetString() ?? "";
                        if (root.TryGetProperty("platformId", out var pid)) platformId = pid.GetString() ?? "";
                        if (root.TryGetProperty("PlatformId", out var pid2)) platformId = pid2.GetString() ?? "";
                        if (root.TryGetProperty("birthday", out var b)) birthday = b.GetString() ?? "";
                        if (root.TryGetProperty("dateOfBirth", out var dob)) { if (string.IsNullOrEmpty(birthday)) birthday = dob.GetString() ?? ""; }
                    }
                    catch (Exception ex) { Console.WriteLine($"[DEBUG /account/create] JSON parse error: {ex.Message}"); }
                }
            }

            Console.WriteLine($"[DEBUG /account/create] Final: username='{username}' displayName='{displayName}' password='{password}' platformId='{platformId}' birthday='{birthday}'");

            // شرط أساسي: يجب أن يكون هناك platformId
            if (string.IsNullOrEmpty(platformId))
            {
                return "{\"success\":false,\"error\":\"platformId is required\"}";
            }

            // NOTE: No auto-creation. The client only calls /account/create when the
            // user actually fills in the start-screen form (Let's Start -> create account).
            // If username is still empty, there is nothing to create — the client has
            // not submitted a real account, so we reject rather than fabricate one.
            if (string.IsNullOrEmpty(username))
            {
                Console.WriteLine($"[DEBUG /account/create] REJECTED: no username supplied. No auto-creation.");
                return "{\"success\":false,\"error\":\"username is required\"}";
            }

            lock (_lock)
            {
                var accounts = AccountHelper.LoadAccounts();

                var takenSet = new HashSet<string>(DatabaseManager.LoadUsernames(), StringComparer.OrdinalIgnoreCase);
                if (takenSet.Contains(username))
                {
                    return "{\"success\":false,\"error\":\"Username already taken\"}";
                }

                string nowFinal = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff");

                // Always create a brand-new serial account. Never match / reuse an
                // existing account by platformId or accountId — even for the same
                // Steam ID or the same device, each create_account is a fresh record.
                int newCompleteId = accounts.Any() ? accounts.Max(a => a.Id) + 1 : 1;
                string tokenComplete = Guid.NewGuid().ToString();
                string refreshTokenComplete = Guid.NewGuid().ToString();
                string finalDisplay = string.IsNullOrEmpty(displayName) ? username : displayName;

                var completeAccount = new Account
                {
                    Id = newCompleteId,
                    PlatformId = platformId,
                    Username = username,
                    Password = password,
                    DisplayName = finalDisplay,
                    Token = tokenComplete,
                    RefreshToken = refreshTokenComplete,
                    IsNew = false,
                    Level = 1,
                    Experience = 0,
                    Tokens = 1000,
                    Gold = 500,
                    ProfileImage = AccountCreateApi.DefaultProfileImage,
                    Bio = "",
                    LastLoginTime = nowFinal,
                    CreatedAt = nowFinal,
                    IsJunior = false,
                    RegistrationStatus = 2,
                    PersonalPronouns = 0,
                    IdentityFlags = 0,
                    DisplayEmoji = null,
                    Birthday = string.IsNullOrEmpty(birthday) ? null : birthday
                };

                accounts.Add(completeAccount);
                AccountHelper.SaveAccounts(accounts);
                DatabaseManager.SavePlayerData(completeAccount);
                DatabaseManager.RegisterUsername(username);

                // Reference: auth.app.ts calls placeNewPlayerInOrientation() at the
                // end of account creation so the brand-new account's presence points
                // at Orientation (room 13, instance -2) instead of no/stale dorm
                // presence, which the heartbeat would otherwise report out-of-sync.
                RoomData.AssignOrientation(newCompleteId);

                Console.WriteLine($"[DEBUG /account/create] Created NEW complete account: id={newCompleteId} username='{username}' platformId='{platformId}'");
                return BuildCompleteResponse(completeAccount);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG /account/create] Error: {ex}");
            return "{\"error\":\"InternalServerError\",\"code\":500}";
        }
    }

    private static string BuildIncompleteResponse(Account acc)
    {
        var response = new Dictionary<string, object>
        {
            { "success", true },
            { "value", new Dictionary<string, object>
                {
                    { "accountId", acc.Id },
                    { "id", acc.Id },
                    { "username", acc.Username ?? "" },
                    { "displayName", acc.DisplayName ?? acc.Username ?? "" },
                    { "token", acc.Token },
                    { "refreshToken", acc.RefreshToken },
                    { "isJunior", acc.IsJunior },
                    { "registrationStatus", 0 },
                    {
                        "platforms",
                        new object[]
                        {
                            new Dictionary<string, object>
                            {
                                { "platform", 0 },
                                { "platformId", acc.PlatformId ?? "" }
                            }
                        }
                    },
                    { "profileImage", acc.ProfileImage },
                    { "createdAt", acc.CreatedAt },
                    { "personalPronouns", acc.PersonalPronouns },
                    { "identityFlags", acc.IdentityFlags },
                    { "displayEmoji", acc.DisplayEmoji }
                }
            }
        };
        return JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = null, PropertyNameCaseInsensitive = true });
    }

    private static string BuildCompleteResponse(Account acc)
    {
        var response = new Dictionary<string, object>
        {
            { "success", true },
            { "value", new Dictionary<string, object>
                {
                    { "accountId", acc.Id },
                    { "id", acc.Id },
                    { "username", acc.Username ?? "" },
                    { "displayName", acc.DisplayName ?? acc.Username ?? "" },
                    { "token", acc.Token },
                    { "refreshToken", acc.RefreshToken },
                    { "isJunior", acc.IsJunior },
                    { "registrationStatus", 2 },
                    {
                        "platforms",
                        new object[]
                        {
                            new Dictionary<string, object>
                            {
                                { "platform", 0 },
                                { "platformId", acc.PlatformId ?? "" }
                            }
                        }
                    },
                    { "profileImage", acc.ProfileImage },
                    { "createdAt", acc.CreatedAt },
                    { "personalPronouns", acc.PersonalPronouns },
                    { "identityFlags", acc.IdentityFlags },
                    { "displayEmoji", acc.DisplayEmoji }
                }
            }
        };
        return JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = null, PropertyNameCaseInsensitive = true });
    }

    public string GetUrl() => "/account/create";
    public string GetMethod() => HttpMethods.Post;
}
