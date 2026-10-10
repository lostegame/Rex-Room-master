namespace Rex_Room_20230414.API;

using Microsoft.AspNetCore.Http;
using System.Text;

public class NameGenOptionsApi : IApi
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

    public string Response(HttpContext context)
    {
        var sb = new StringBuilder();
        sb.Append("{\"Adjectives\":[");
        for (int i = 0; i < Adjectives.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"').Append(Adjectives[i]).Append('"');
        }
        sb.Append("],\"Nouns\":[");
        for (int i = 0; i < Nouns.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"').Append(Nouns[i]).Append('"');
        }
        sb.Append("]}");
        return sb.ToString();
    }

    public string GetUrl() => "/namegen/options";
    public string GetMethod() => HttpMethods.Get;
}