namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class PlayerLogoutApi : IApi
{
    public string Response(HttpContext context)
    {
        // Reference (match.app.ts /player/logout): the client fires a SPURIOUS
        // logout right after the Orientation seed, so "a logout whose presence still
        // points at Orientation is left as a no-op ack" - clearing presence there
        // wipes the seed and bounces the new player to the dorm. Every other logout
        // clears the assignment normally, and an unauthenticated one is a no-op.
        if (RoomData.IsOrientationAssigned(context))
        {
            int seededAccount = AccountHelper.GetAccountIdFromToken(context) ?? 0;
            Console.WriteLine($"[logout] account={seededAccount} presence is Orientation - " +
                              "leaving the seed in place instead of clearing it");
            return "";
        }

        RoomData.ClearRoom(context);

        // Forget the reward entry marker too, so the next login is treated as a
        // fresh entry and pays out again. Without this a relog into the same
        // instance id would look like a repeat entry and pay nothing.
        int accountId = AccountHelper.GetAccountIdFromToken(context) ?? 0;
        if (accountId != 0) JoinReward.Reset(accountId);

        return "";
    }

    public string GetUrl() => "/player/logout";
    public string GetMethod() => HttpMethods.Post;
}
