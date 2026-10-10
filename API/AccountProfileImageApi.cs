namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Text.Json;

public class AccountProfileImageApi : IApi
{
    public string Response(HttpContext context)
    {
        var accountId = AccountHelper.GetAccountIdFromToken(context);
        if (accountId == null) return "{\"error\":\"Unauthorized\"}";

        // The client posts "imageName=sharecamera%2F...jpg" as form-urlencoded
        // text, frequently with NO Content-Type header. Keying off
        // HasFormContentType therefore failed and the avatar never changed, so
        // parse the raw body instead of trusting the header.
        string imageName = AccountHelper.FormValue(context, "imageName", "profileImage");

        var accounts = AccountHelper.LoadAccounts();
        var acc = accounts.FirstOrDefault(a => a.Id == accountId.Value);
        if (acc == null) return "{\"error\":\"AccountNotFound\"}";

        if (!string.IsNullOrEmpty(imageName))
        {
            acc.ProfileImage = imageName;
            acc.IsNew = false;
            AccountHelper.SaveAccounts(accounts);
            Console.WriteLine($"[ProfileImage] account={accountId.Value} imageName={imageName}");
        }

        return "{\"success\":true}";
    }

    public string GetUrl() => "/account/me/profileimage";
    public string GetMethod() => HttpMethods.Put;
}