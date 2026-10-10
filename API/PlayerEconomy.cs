namespace Rex_Room_20230414.API;

using System;
using System.Collections.Generic;
using System.Linq;

// Real player economy: tokens and invention ownership.
//
// Before this existed the balance was fiction:
//   - Account.Tokens was on the model but NOTHING ever read or wrote it.
//   - GET /api/storefronts/v4/balance/2 hardcoded "Balance":10000 for everyone.
//   - GET /api/roomcurrencies/v1/getAllBalances returned [].
//   - Buying an invention always answered UpdateResponse 303 (declined).
//
// So "I can't buy anything" was not a client bug: the server had no balance to
// spend and refused every purchase. This class makes both sides real and
// persisted, so a purchase survives a restart and the UI can reflect it.

public static class PlayerEconomy
{
    // Tokens handed out each time a player enters a room (asked for explicitly).
    public const int JoinRewardTokens = 100;

    // XP granted per room entry. The 2023 client never calls
    // POST /api/players/v2/addxp, so nothing ever moved XP before and the
    // level-up gift drop could not fire. This drives it from real activity.
    public const int JoinRewardXp = 25;

    private static readonly object _lock = new();

    public static int Balance(int accountId)
    {
        if (accountId <= 0) return 0;
        try
        {
            var acc = AccountHelper.GetAccountById(accountId);
            return acc?.Tokens ?? 0;
        }
        catch { return 0; }
    }

    // Adds tokens. Returns the new balance, or -1 if the account is unknown.
    public static int AddTokens(int accountId, int amount)
    {
        if (accountId <= 0 || amount == 0) return -1;
        lock (_lock)
        {
            try
            {
                var acc = AccountHelper.GetAccountById(accountId);
                if (acc == null) return -1;
                acc.Tokens += amount;
                if (acc.Tokens < 0) acc.Tokens = 0;
                AccountHelper.SaveAccount(acc);
                return acc.Tokens;
            }
            catch { return -1; }
        }
    }

    public enum SpendResult { Ok, InsufficientFunds, UnknownAccount }

    // Atomically checks and deducts. Never lets a balance go negative.
    public static SpendResult TrySpend(int accountId, int amount, out int newBalance)
    {
        newBalance = 0;
        if (accountId <= 0) return SpendResult.UnknownAccount;
        if (amount < 0) return SpendResult.Ok;
        lock (_lock)
        {
            try
            {
                var acc = AccountHelper.GetAccountById(accountId);
                if (acc == null) return SpendResult.UnknownAccount;
                if (acc.Tokens < amount) { newBalance = acc.Tokens; return SpendResult.InsufficientFunds; }
                acc.Tokens -= amount;
                AccountHelper.SaveAccount(acc);
                newBalance = acc.Tokens;
                return SpendResult.Ok;
            }
            catch { return SpendResult.UnknownAccount; }
        }
    }

    // ---- Invention ownership -------------------------------------------------

    public static bool OwnsInvention(int accountId, int inventionId)
    {
        if (accountId <= 0 || inventionId <= 0) return false;
        try
        {
            var acc = AccountHelper.GetAccountById(accountId);
            return acc?.OwnedInventionIds != null && acc.OwnedInventionIds.Contains(inventionId);
        }
        catch { return false; }
    }

    public static List<int> OwnedInventions(int accountId)
    {
        if (accountId <= 0) return new List<int>();
        try
        {
            var acc = AccountHelper.GetAccountById(accountId);
            return acc?.OwnedInventionIds != null
                ? new List<int>(acc.OwnedInventionIds)
                : new List<int>();
        }
        catch { return new List<int>(); }
    }

    // Returns true when ownership was newly granted (false if already owned).
    public static bool GrantInvention(int accountId, int inventionId)
    {
        if (accountId <= 0 || inventionId <= 0) return false;
        lock (_lock)
        {
            try
            {
                var acc = AccountHelper.GetAccountById(accountId);
                if (acc == null) return false;
                if (acc.OwnedInventionIds == null) acc.OwnedInventionIds = new List<int>();
                if (acc.OwnedInventionIds.Contains(inventionId)) return false;
                acc.OwnedInventionIds.Add(inventionId);
                AccountHelper.SaveAccount(acc);
                return true;
            }
            catch { return false; }
        }
    }
}
