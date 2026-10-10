using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
namespace Rex_Room_20230414.API
{
    public class AccountBulkApi : IApi
    {
        public string GetUrl() => "/account/bulk";
        public string GetMethod() => "GET";

        public string Response(HttpContext context)
        {
            var ids = context.Request.Query["id"];
            var responseList = new List<object>();

            var accounts = AccountHelper.LoadAccounts();

            foreach (var idStr in ids)
            {
                if (int.TryParse(idStr, out int id))
                {
                    var account = accounts.FirstOrDefault(a => a.Id == id);
                    if (account != null)
                    {
                        responseList.Add(new Dictionary<string, object>
                        {
                            { "accountId", account.Id },
                            { "username", account.Username ?? "" },
                            { "displayName", account.DisplayName ?? account.Username ?? "" },
                            { "profileImage", account.ProfileImage ?? "" },
                            { "bannerImage", "" },
                            { "displayEmoji", account.DisplayEmoji ?? "" },
                            { "isJunior", account.IsJunior },
                            { "platforms", 0 },
                            { "personalPronouns", account.PersonalPronouns },
                            { "identityFlags", account.IdentityFlags },
                            { "createdAt", account.CreatedAt ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff") }
                        });
                    }
                }
            }

            return JsonSerializer.Serialize(responseList);
        }
    }
}
