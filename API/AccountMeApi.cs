namespace Rex_Room_20230414.API;

using Microsoft.AspNetCore.Http;
using System;
using System.Text.Json;

public class AccountMeApi : IApi
{
    public string Response(HttpContext context)
    {
        try
        {
            var accountId = AccountHelper.GetAccountIdFromToken(context);
            if (accountId == null)
                return "{\"error\":\"Unauthorized\"}";

            var acc = AccountHelper.GetAccountById(accountId.Value);
            if (acc == null)
                return "{\"error\":\"AccountNotFound\"}";

            string profileImage = !string.IsNullOrEmpty(acc.ProfileImage) ? acc.ProfileImage : null;
            string displayName = !string.IsNullOrEmpty(acc.DisplayName) ? acc.DisplayName : acc.Username;
            bool treatAsJunior = string.IsNullOrEmpty(acc.Birthday);
            return $@"{{
  ""accountId"":{acc.Id},
  ""username"":""{Escape(acc.Username)}"",
  ""displayName"":""{Escape(displayName)}"",
  ""profileImage"":{(profileImage == null ? "null" : $"\"{profileImage}\"")},
  ""bannerImage"":"""",
  ""displayEmoji"":{(acc.DisplayEmoji == null ? "null" : $"\"{Escape(acc.DisplayEmoji)}\"")},
  ""isJunior"":{(treatAsJunior ? "null" : acc.IsJunior.ToString().ToLower())},
  ""treatAsJunior"":{treatAsJunior.ToString().ToLower()},
  ""platforms"":0,
  ""personalPronouns"":{acc.PersonalPronouns},
  ""identityFlags"":{acc.IdentityFlags},
  ""createdAt"":""{AccountHelper.IsoUtc(acc.CreatedAt)}"",
  ""email"":""{Escape(acc.Email ?? "")}"",
  ""phone"":""{Escape(acc.Phone ?? "")}"",
  ""birthday"":{(string.IsNullOrEmpty(acc.Birthday) ? "null" : $"\"{Escape(acc.Birthday)}\"")},
  ""registrationStatus"":{acc.RegistrationStatus},
  ""isNew"":{acc.IsNew.ToString().ToLower()},
  ""availableUsernameChanges"":1
}}";
        }
        catch (Exception ex)
        {
            return "{\"error\":\"" + ex.Message.Replace("\"", "'") + "\"}";
        }
    }

    private static string Escape(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

    public string GetUrl() => "/account/me";
    public string GetMethod() => HttpMethods.Get;
}
