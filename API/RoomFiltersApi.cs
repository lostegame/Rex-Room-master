namespace Rex_Room_20230414.API;
using Microsoft.AspNetCore.Http;

public class RoomFiltersApi : IApi
{
    public string Response(HttpContext context) =>
        @"{
  ""PinnedFilters"":[""recroomoriginal"",""community"",""featured"",""quest"",""pvp"",""hangout"",""game"",""art"",""store"",""tutorial"",""fandom"",""performance"",""action"",""horror""],
  ""PopularFilters"":[""pvp"",""quest"",""game"",""hangout"",""art""],
  ""TrendingFilters"":[""roleplay"",""nomp"",""rp"",""casual"",""fun"",""action"",""military"",""sports""]
}";

    public string GetUrl() => "/api/rooms/v1/filters";
    public string GetMethod() => HttpMethods.Get;
}
