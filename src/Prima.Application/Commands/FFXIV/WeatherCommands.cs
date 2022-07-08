using System.Text;
using Discord;
using Discord.Commands;
using FFXIVWeather.Lumina;
using Lumina.Excel.GeneratedSheets;
using Prima.DiscordNet.Attributes;
using Prima.Resources;
using Prima.Services;
using TimeZoneNames;
using Color = Discord.Color;

namespace Prima.Application.Commands.FFXIV;

[Name("FFXIV Weather")]
public class WeatherCommands : ModuleBase<SocketCommandContext>
{
    private const ulong CEMSpeculation = 738899820168740984;
    private const ulong CEMBozTheorycrafting = 593815337980526603;

    private readonly IDbService _db;
    private readonly FFXIVWeatherLuminaService _weather;

    public WeatherCommands(IDbService db, FFXIVWeatherLuminaService weather)
    {
        _db = db;
        _weather = weather;
    }

    [Command("weather")]
    [Description("[FFXIV] Shows the current weather for the specified zone.")]
    [DisableInChannelsForGuild(CEMSpeculation, CEMBozTheorycrafting,
        GuildId = SpecialGuilds.CrystalExploratoryMissions)]
    public async Task WeatherAsync([Remainder] string zone)
    {
        IList<(Weather, DateTime)> forecast;
        try
        {
            forecast = _weather.GetForecast(zone, count: 10);
        }
        catch (ArgumentException)
        {
            await ReplyAsync("The specified zone could not be found.");
            return;
        }

        var (currentWeather, currentWeatherStartTime) = forecast[0];

        var dbUser = _db.Users.FirstOrDefault(u => u.DiscordId == Context.User.Id);
        var (customTzi, _) = Util.GetLocalizedTimeForUser(dbUser, DateTime.Now);
        var tzi = customTzi ?? TimeZoneInfo.FindSystemTimeZoneById(Util.PtIdString());

        var tzAbbrs = TZNames.GetAbbreviationsForTimeZone(tzi.Id, "en-US");
        var tzAbbr = tzi.IsDaylightSavingTime(DateTime.Now) ? tzAbbrs.Daylight : tzAbbrs.Standard;

        var formattedForecast =
            $"**Current:** {currentWeather.Name} (Began at {TimeZoneInfo.ConvertTimeFromUtc(currentWeatherStartTime, tzi).ToShortTimeString()} {tzAbbr})";
        foreach (var (weather, startTime) in forecast.Skip(1))
        {
            var zonedTime = TimeZoneInfo.ConvertTimeFromUtc(startTime, tzi);

            formattedForecast += $"\n{zonedTime.ToShortTimeString()}: {weather.Name}";
        }

        var embed = new EmbedBuilder()
            .WithAuthor(new EmbedAuthorBuilder()
                .WithIconUrl(
                    $"https://www.garlandtools.org/files/icons/weather/{currentWeather.Name.ToString().Replace(" ", "%20")}.png")
                .WithName($"Current weather for {Util.JadenCase(zone)}:"))
            .WithTitle(
                $"Next weather starts in {Math.Truncate((forecast[1].Item2 - DateTime.UtcNow).TotalMinutes)} minutes.")
            .WithColor(Color.LightOrange)
            .WithDescription(formattedForecast)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("weatherreport")]
    [Description("[FFXIV] Provides a text file with the next 200 weather entries for the specified zone.")]
    [DisableInChannelsForGuild(CEMSpeculation, CEMBozTheorycrafting,
        GuildId = SpecialGuilds.CrystalExploratoryMissions)]
    public async Task WeatherReportAsync([Remainder] string zone)
    {
        IList<(Weather, DateTime)> forecast;
        try
        {
            forecast = _weather.GetForecast(zone, count: 200);
        }
        catch (ArgumentException)
        {
            await ReplyAsync("The specified zone could not be found.");
            return;
        }

        var dbUser = _db.Users.FirstOrDefault(u => u.DiscordId == Context.User.Id);
        var (customTzi, _) = Util.GetLocalizedTimeForUser(dbUser, DateTime.Now);
        var tzi = customTzi ?? TimeZoneInfo.FindSystemTimeZoneById(Util.PtIdString());

        var tzAbbrs = TZNames.GetAbbreviationsForTimeZone(tzi.Id, "en-US");
        var tzAbbr = tzi.IsDaylightSavingTime(DateTime.Now) ? tzAbbrs.Daylight : tzAbbrs.Standard;

        var outputData = forecast
            .Aggregate($"Time\t\t\t\tWeather\n{new string('=', "7/4/2021 6:13:20 PM PDT         Dust Storms".Length)}",
                (agg, next) =>
                {
                    var (weather, startTime) = next;
                    var zonedTime = TimeZoneInfo.ConvertTimeFromUtc(startTime, tzi);
                    var timeText = zonedTime + " " + tzAbbr;
                    return agg + $"\n{timeText}{(timeText.Length < 24 ? "\t" : "")}\t{weather.Name}";
                });

        await using var file = new MemoryStream(Encoding.UTF8.GetBytes(outputData));

        await Context.Channel.SendFileAsync(file, "weather.txt",
            messageReference: new MessageReference(Context.Message.Id));
    }
}