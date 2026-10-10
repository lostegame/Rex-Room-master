namespace Rex_Room_20230414.API;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rex_Room_20230414.Database;

public static class RandomUsername
{
    private static readonly string[] Adjectives =
    {
        "Blazing","Bold","Brave","Breezy","Chill","Cosmic","Crazy","Cyber","Dizzy","Electric",
        "Epic","Fierce","Frosty","Funky","Fuzzy","Gentle","Golden","Grumpy","Happy","Jazzy",
        "Jolly","Loyal","Lucky","Mighty","Neon","Nimble","Peppy","Plucky","Quantum","Radiant",
        "Rapid","Retro","Royal","Rusty","Shiny","Silent","Sly","Snazzy","Sneaky","Spicy",
        "Stormy","Sturdy","Sunny","Turbo","Vibrant","Vivid","Wicked","Wild","Witty","Zesty"
    };

    private static readonly string[] Nouns =
    {
        "Bear","Bolt","Byte","Cheetah","Cobra","Comet","Dolphin","Dragon","Eagle","Falcon",
        "Flame","Fox","Frost","Ghost","Goblin","Griffin","Hawk","Jaguar","Knight","Lion",
        "Lynx","Meteor","Moon","Ninja","Nova","Otter","Panda","Panther","Penguin","Phoenix",
        "Pirate","Pixel","Raven","Rocket","Robot","Shark","Shadow","Sky","Spark","Sphinx",
        "Star","Storm","Sun","Tiger","Viper","Wave","Wizard","Wolf","Yeti","Goblin"
    };

    private static readonly Random _random = new Random();

    /// <summary>
    /// Returns a free username. Prefers names from random_usernames_500.txt that are not yet taken;
    /// falls back to generated Adjective+Noun+4digit names when the list is exhausted (or missing).
    /// </summary>
    public static string GetFreeUsername()
    {
        var taken = new HashSet<string>(DatabaseManager.LoadUsernames(), StringComparer.OrdinalIgnoreCase);
        foreach (var acc in DatabaseManager.LoadAllPlayerData())
            if (!string.IsNullOrEmpty(acc.Username)) taken.Add(acc.Username);

        var list = TryLoadList();
        if (list != null)
        {
            var free = list.Where(n => !taken.Contains(n)).ToList();
            if (free.Count > 0)
                return free[_random.Next(free.Count)];
        }

        for (int attempt = 0; attempt < 200; attempt++)
        {
            string candidate = Adjectives[_random.Next(Adjectives.Length)] + Nouns[_random.Next(Nouns.Length)] + _random.Next(1000, 10000);
            if (!taken.Contains(candidate)) return candidate;
        }
        return "Player" + _random.Next(1000, 9999);
    }

    private static List<string> TryLoadList()
    {
        string[] candidates =
        {
            "random_usernames_500.txt",
            Path.Combine(AppContext.BaseDirectory, "random_usernames_500.txt"),
            Path.Combine(Directory.GetCurrentDirectory(), "random_usernames_500.txt"),
            "random_usernames_500.json"
        };
        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                try
                {
                    var lines = File.ReadAllLines(path)
                        .Select(l => l.Trim())
                        .Where(l => l.Length > 0 && !l.StartsWith("["))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (lines.Count > 0) return lines;
                }
                catch { }
            }
        }
        return null;
    }
}