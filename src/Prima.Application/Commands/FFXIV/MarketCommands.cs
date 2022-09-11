using Discord.Commands;
using Newtonsoft.Json.Linq;
using Prima.DiscordNet.Attributes;
using Prima.Game.FFXIV.XIVAPI;

namespace Prima.Application.Commands.FFXIV;

[Name("FFXIV Market")]
public class MarketCommands : ModuleBase<SocketCommandContext>
{
    private readonly HttpClient _http;
    private readonly XIVAPIClient _xivapi;

    public MarketCommands(HttpClient http, XIVAPIClient xivapi)
    {
        _http = http;
        _xivapi = xivapi;
    }

    [Command("market", RunMode = RunMode.Async)]
    [Description("[FFXIV] Look up market data for an item. Usage: `~market <item name> <world>`")]
    public async Task MarketAsync(params string[] args)
    {
        if (args.Length < 2)
        {
            await ReplyAsync(
                $"{Context.User.Mention}, please provide an item name in your command, followed by the World or DC name to query.");
            return;
        }

        var itemName = string.Join(' ', args[0..^2]);
        var worldName = args[^1];
        worldName = char.ToUpper(worldName[0]) + worldName[1..];

        var searchResults = await _xivapi.SearchItem(itemName);
        if (searchResults.Count == 0)
        {
            await ReplyAsync($"No results found for \"{itemName}\", are you sure you spelled the item name correctly?");
            return;
        }

        var searchData = searchResults
            .Where(result => string.Equals(result.Name, itemName, StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        var item = !searchData.Any() ? searchResults.First() : searchData.First();

        var itemId = item.Id;
        itemName = item.Name;

        var uniResponse = await _http.GetAsync(new Uri($"https://universalis.app/api/{worldName}/{itemId}"));
        var dataObject = await uniResponse.Content.ReadAsStringAsync();
        var listings = JObject.Parse(dataObject)["Results"].ToObject<IList<UniversalisListing>>();
        var trimmedListings = listings.Take(Math.Min(10, listings.Count())).ToList();

        await ReplyAsync($"__{listings.Count} results for {worldName} (Showing up to 10):__\n" +
                         trimmedListings.Select(listing => listing.Quantity + " **" + itemName + "** for " +
                                                           listing.PricePerUnit + " Gil " +
                                                           (!string.IsNullOrEmpty(listing.WorldName)
                                                               ? "on " +
                                                                 listing.WorldName + " "
                                                               : "") + (listing.Quantity > 1
                                                               ? $" (For a total of {listing.Total} Gil)"
                                                               : "")));
    }

    public class UniversalisListing
    {
        public bool Hq { get; set; }
        public int PricePerUnit { get; set; }
        public int Quantity { get; set; }
        public string RetainerName { get; set; }
        public int Total { get; set; }
        public string WorldName { get; set; }
    }
}