using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using Prima.DiscordNet.Attributes;
using Prima.Game.FFXIV.XIVAPI;
using Prima.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Color = Discord.Color;

namespace Prima.Stable.Modules
{
    [Name("Extra")]
    public class ExtraModule : ModuleBase<SocketCommandContext>
    {
        public IDbService Db { get; set; }
        public HttpClient Http { get; set; }
        public XIVAPIClient Xivapi { get; set; }

        [Command("roll", RunMode = RunMode.Async)]
        [Description("T̵̪͖̎̈̍ḛ̷̤͑̚ș̴̔͑̾ͅͅt̸͔͜͝ ̶̡̨̪͌̉͠ỷ̵̺̕o̴̞̍ū̴͚̣̤r̵͚͎͔͘ ̴̨̬̿ḷ̷͖̀̚u̴͖̲͌́c̴̲̣͙͑̈͝k̸͍͖̿̆̓!̶̢̅̀")]
        public async Task RollAsync([Remainder] string args = "")
        {
            var res = (int)Math.Floor(new Random().NextDouble() * 4);
            switch (res)
            {
                case 0:
                    await ReplyAsync($"BINGO! You matched {(int)Math.Floor(new Random().NextDouble() * 11) + 1} lines! Congratulations!");
                    break;
                case 1:
                    var opt = (int)Math.Floor(new Random().NextDouble() * 2598960) + 1;
                    if (opt <= 4)
                        await ReplyAsync("JACK**P**O*T!* Roya**l flush!** You __won__ [%#*(!@] credits*!*");
                    else if (opt is > 4 and <= 40)
                        await ReplyAsync("Straight flush! You won [TypeError: Cannot read property 'MUNZ' of undefined] credits!");
                    else if (opt is > 40 and <= 664)
                        await ReplyAsync("Four of a kind! You won [TypeError: Cannot read property 'thinking' of undefined] credits!");
                    else if (opt is > 664 and <= 4408)
                        await ReplyAsync("Full house! You won [TypeError: Cannot read property '<:GWgoaThinken:582982105282379797>' of undefined] credits!");
                    else if (opt is > 4408 and <= 9516)
                        await ReplyAsync("Flush! You won -1 credits!");
                    else if (opt is > 9516 and <= 19716)
                        await ReplyAsync("Straight! You won -20 credits!");
                    else if (opt is > 19716 and <= 74628)
                        await ReplyAsync("Two pairs...? You won -500 credits!");
                    else if (opt is > 198180 and <= 1296420)
                        await ReplyAsync("One pair. You won -2500 credits.");
                    else
                        await ReplyAsync("No pairs. You won -10000 credits.");
                    break;
                case 2:
                    await ReplyAsync($"Critical hit! You dealt {(int)Math.Floor(new Random().NextDouble() * 9999) + 9999} damage!");
                    break;
                case 3:
                    await ReplyAsync("You took 9999999 points of damage. You lost the game.");
                    break;
            }
        }

        [Command("market", RunMode = RunMode.Async)]
        [Description("[FFXIV] Look up market data for an item. Usage: `~market <item name> <world>`")]
        public async Task MarketAsync(params string[] args)
        {
            if (args.Length < 2)
            {
                await ReplyAsync($"{Context.User.Mention}, please provide an item name in your command, followed by the World or DC name to query.");
                return;
            }

            var itemName = string.Join(' ', args[0..^2]);
            var worldName = args[^1];
            worldName = char.ToUpper(worldName[0]) + worldName[1..];

            var searchResults = await Xivapi.SearchItem(itemName);
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

            var uniResponse = await Http.GetAsync(new Uri($"https://universalis.app/api/{worldName}/{itemId}"));
            var dataObject = await uniResponse.Content.ReadAsStringAsync();
            var listings = JObject.Parse(dataObject)["Results"].ToObject<IList<UniversalisListing>>();
            var trimmedListings = listings.Take(Math.Min(10, listings.Count())).ToList();

            await ReplyAsync($"__{listings.Count} results for {worldName} (Showing up to 10):__\n" +
                trimmedListings.Select(listing => listing.Quantity + " **" + itemName + "** for " +
                    listing.PricePerUnit + " Gil " + (!string.IsNullOrEmpty(listing.WorldName) ? "on " +
                    listing.WorldName + " " : "") + (listing.Quantity > 1 ? $" (For a total of {listing.Total} Gil)" : "")));
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

        [Command("setdescription")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task SetDescriptionAsync([Remainder] string description)
        {
            await Db.DeleteChannelDescription(Context.Channel.Id);
            await Db.AddChannelDescription(Context.Channel.Id, description);
            await ReplyAsync($"{Context.User.Mention}, the help message has been updated!");
        }

        [Command("whatisthis")]
        [Description("Explains what the channel you use it in is for, if such information is available.")]
        public Task WhatIsThisAsync()
        {
            var cd = Db.ChannelDescriptions.FirstOrDefault(cd => cd.ChannelId == Context.Channel.Id);
            if (cd == null) return Task.CompletedTask;
            var embed = new EmbedBuilder()
                .WithTitle($"#{Context.Channel.Name}")
                .WithColor(new Color(0x00, 0x80, 0xFF))
                .WithThumbnailUrl("http://www.newdesignfile.com/postpic/2016/05/windows-8-help-icon_398417.png")
                .WithDescription(cd.Description)
                .Build();
            return ReplyAsync(embed: embed);
        }
    }
}
