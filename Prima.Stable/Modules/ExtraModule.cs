using Discord.Commands;
using Newtonsoft.Json.Linq;
using Prima.Services;
using Prima.XIVAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Prima.Attributes;
using FFXIVWeather;
using FFXIVWeather.Models;
using Discord;
using Prima.Resources;
using TimeZoneNames;
using Color = Discord.Color;

namespace Prima.Extra.Modules
{
    [Name("Extra")]
    public class ExtraModule : ModuleBase<SocketCommandContext>
    {
        public DbService Db { get; set; }
        public FFXIVWeatherService Weather { get; set; }
        public HttpClient Http { get; set; }
        public XIVAPIService Xivapi { get; set; }

        [Command("weather")]
        [Description("[FFXIV] Shows the current weather for the specified zone.")]
        public async Task WeatherAsync([Remainder] string zone)
        {
            IList<(Weather, DateTime)> forecast;
            try
            {
                forecast = Weather.GetForecast(zone, count: 10);
            }
            catch (ArgumentException)
            {
                await ReplyAsync("The specified zone could not be found.");
                return;
            }

            var (currentWeather, currentWeatherStartTime) = forecast[0];

            var dbUser = Db.Users.FirstOrDefault(u => u.DiscordId == Context.User.Id);
            var tzi = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
            var (customTzi, _) = Util.GetLocalizedTimeForUser(dbUser, DateTime.Now);
            if (customTzi != null)
            {
                tzi = customTzi;
            }

            var tzAbbrs = TZNames.GetAbbreviationsForTimeZone(tzi.Id, CultureInfo.CurrentCulture.Name);
            var tzAbbr = tzi.IsDaylightSavingTime(DateTime.Now) ? tzAbbrs.Daylight : tzAbbrs.Standard;

            var formattedForecast = $"**Current:** {currentWeather} (Began at {TimeZoneInfo.ConvertTimeFromUtc(currentWeatherStartTime, tzi).ToShortTimeString()} {tzAbbr})";
            foreach (var (weather, startTime) in forecast.Skip(1))
            {
                var zonedTime = TimeZoneInfo.ConvertTimeFromUtc(startTime, tzi);

                formattedForecast += $"\n{zonedTime.ToShortTimeString()}: {weather}";
            }

            var embed = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithIconUrl($"https://www.garlandtools.org/files/icons/weather/{currentWeather.GetName().Replace(" ", "%20")}.png")
                    .WithName($"Current weather for {Util.JadenCase(zone)}:"))
                .WithTitle($"Next weather starts in {Math.Truncate((forecast[1].Item2 - DateTime.UtcNow).TotalMinutes)} minutes.")
                .WithColor(Color.LightOrange)
                .WithDescription(formattedForecast)
                .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("roll", RunMode = RunMode.Async)]
        [Description("T̵̪͖̎̈̍ḛ̷̤͑̚ș̴̔͑̾ͅͅt̸͔͜͝ ̶̡̨̪͌̉͠ỷ̵̺̕o̴̞̍ū̴͚̣̤r̵͚͎͔͘ ̴̨̬̿ḷ̷͖̀̚u̴͖̲͌́c̴̲̣͙͑̈͝k̸͍͖̿̆̓!̶̢̅̀")]
        public async Task RollAsync()
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
                    else if (opt > 4 && opt <= 40)
                        await ReplyAsync("Straight flush! You won [TypeError: Cannot read property 'MUNZ' of undefined] credits!");
                    else if (opt > 40 && opt <= 664)
                        await ReplyAsync("Four of a kind! You won [TypeError: Cannot read property 'thinking' of undefined] credits!");
                    else if (opt > 664 && opt <= 4408)
                        await ReplyAsync("Full house! You won [TypeError: Cannot read property '<:GWgoaThinken:582982105282379797>' of undefined] credits!");
                    else if (opt > 4408 && opt <= 9516)
                        await ReplyAsync("Flush! You won -1 credits!");
                    else if (opt > 9516 && opt <= 19716)
                        await ReplyAsync("Straight! You won -20 credits!");
                    else if (opt > 19716 && opt <= 74628)
                        await ReplyAsync("Two pairs...? You won -500 credits!");
                    else if (opt > 198180 && opt <= 1296420)
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
            worldName = char.ToUpper(worldName[0]) + worldName.Substring(1);

            var searchResults = await Xivapi.Search<Item>(itemName);
            if (searchResults.Count == 0)
            {
                await ReplyAsync($"No results found for \"{itemName}\", are you sure you spelled the item name correctly?");
                return;
            }
            var searchData = searchResults.Where(result => result.Name.ToLower() == itemName.ToLower());
            var item = !searchData.Any() ? searchResults.First() : searchData.First();

            var itemId = item.ID;
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

        [Command("portals", RunMode = RunMode.Async)]
        [Description("Shows the portal map.")]
        [RateLimit(TimeSeconds = 30, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task PortalsAsync()
        {
            using var http = new HttpClient();
            var fileStream = await http.GetStreamAsync(new Uri("https://i.imgur.com/XcXACQp.png"));
            await Context.Channel.SendFileAsync(fileStream, "XcXACQp.png");
        }

        [Command("owain", RunMode = RunMode.Async)]
        [Description("Shows the Owain guide.")]
        [RateLimit(TimeSeconds = 120, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task OwainAsync()
        {
            using var http = new HttpClient();
            var fileStream = await http.GetStreamAsync(new Uri("https://i.imgur.com/ADwopqC.jpg"));
            await Context.Channel.SendFileAsync(fileStream, "unknown-2.png");
        }

        [Command("art", RunMode = RunMode.Async)]
        [Description("Shows the Art guide.")]
        [RateLimit(TimeSeconds = 120, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task ArtAsync()
        {
            using var http = new HttpClient();
            var fileStream = await http.GetStreamAsync(new Uri("https://i.imgur.com/sehs4Tw.jpg"));
            await Context.Channel.SendFileAsync(fileStream, "unknown-1.png");
        }

        [Command("raiden", RunMode = RunMode.Async)]
        [Description("Shows the Raiden guide.")]
        [RateLimit(TimeSeconds = 120, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task RaidenAsync()
        {
            using var http = new HttpClient();
            var fileStream = await http.GetStreamAsync(new Uri("https://cdn.discordapp.com/attachments/588592729609469952/721522493197779035/unknown.png"));
            await Context.Channel.SendFileAsync(fileStream, "unknown.png");
        }

        [Command("av", RunMode = RunMode.Async)]
        [Description("Shows the Absolute Virtue guide.")]
        [RateLimit(TimeSeconds = 120, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task AbsoluteVirtueAsync()
        {
            using var http = new HttpClient();
            var fileStream = await http.GetStreamAsync(new Uri("https://cdn.discordapp.com/attachments/588592729609469952/721522585866731580/unknown.png"));
            await Context.Channel.SendFileAsync(fileStream, "unknown.png");
        }

        [Command("ozma", RunMode = RunMode.Async)]
        [Description("Shows the Ozma guide.")]
        [RateLimit(TimeSeconds = 120, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task OzmaAsync()
        {
            using var http = new HttpClient();
            var fileStream = await http.GetStreamAsync(new Uri("https://cdn.discordapp.com/attachments/588592729609469952/721522648949063730/unknown.png"));
            await Context.Channel.SendFileAsync(fileStream, "unknown.png");
        }

        [Command("accel", RunMode = RunMode.Async)]
        [Description("Shows the Acceleration Bomb guide.")]
        [RateLimit(TimeSeconds = 120, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task AccelBombAsync()
        {
            using var http = new HttpClient();
            var fileStream = await http.GetStreamAsync(new Uri("https://cdn.discordapp.com/attachments/550709673104375818/721527266441560064/unknown.png"));
            await Context.Channel.SendFileAsync(fileStream, "unknown.png");
        }
    }
}
