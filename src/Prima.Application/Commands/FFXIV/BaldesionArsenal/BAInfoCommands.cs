using Discord;
using Discord.Commands;
using Prima.DiscordNet;
using Prima.DiscordNet.Attributes;
using Prima.Resources;
using Prima.Services;

namespace Prima.Application.Commands.FFXIV.BaldesionArsenal;

[Name("Baldesion Arsenal Info")]
public class BAInfoCommands : ModuleBase<SocketCommandContext>
{
    private readonly IDbService _db;
    private readonly HttpClient _http;

    public BAInfoCommands(IDbService db, HttpClient http)
    {
        _db = db;
        _http = http;
    }

    [Command("bahelp", RunMode = RunMode.Async)]
    [Description("Shows help information for the extra BA commands.")]
    [RateLimit(TimeSeconds = 10, Global = true)]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public async Task BAHelpAsync()
    {
        var guildConfig = _db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
        var prefix = _db.Config.Prefix.ToString();
        if (guildConfig != null && guildConfig.Prefix != ' ')
            prefix = guildConfig.Prefix.ToString();

        var baseCommands =
            DiscordUtilities.GetFormattedCommandList(typeof(BAInfoCommands), prefix,
                except: new List<string> { "bahelp" });
        var hostCommands = DiscordUtilities.GetFormattedCommandList(typeof(BARunCommands), prefix);

        var embed = new EmbedBuilder()
            .WithTitle("Useful Commands (Baldesion Arsenal)")
            .WithColor(Discord.Color.LightOrange)
            .WithDescription(baseCommands + "==============================\n" + hostCommands)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("portals", RunMode = RunMode.Async)]
    [Description("Shows the portal map.")]
    [RateLimit(TimeSeconds = 10, Global = true)]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public Task PortalsAsync() => DiscordUtilities.PostImage(_http, Context, "https://i.imgur.com/XcXACQp.png");

    [Command("owain", RunMode = RunMode.Async)]
    [Description("Shows the Owain guide.")]
    [RateLimit(TimeSeconds = 10, Global = true)]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public Task OwainAsync() => DiscordUtilities.PostImage(_http, Context, "https://i.imgur.com/ADwopqC.jpg");

    [Command("art", RunMode = RunMode.Async)]
    [Description("Shows the Art guide.")]
    [RateLimit(TimeSeconds = 10, Global = true)]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public Task ArtAsync() => DiscordUtilities.PostImage(_http, Context, "https://i.imgur.com/sehs4Tw.jpg");

    [Command("raiden", RunMode = RunMode.Async)]
    [Description("Shows the Raiden guide.")]
    [RateLimit(TimeSeconds = 10, Global = true)]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public Task RaidenAsync() => DiscordUtilities.PostImage(_http, Context,
        "https://cdn.discordapp.com/attachments/588592729609469952/721522493197779035/unknown.png");

    [Command("av", RunMode = RunMode.Async)]
    [Description("Shows the Absolute Virtue guide.")]
    [RateLimit(TimeSeconds = 10, Global = true)]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public Task AbsoluteVirtueAsync() => DiscordUtilities.PostImage(_http, Context,
        "https://cdn.discordapp.com/attachments/588592729609469952/721522585866731580/unknown.png");

    [Command("ozma", RunMode = RunMode.Async)]
    [Description("Shows the Ozma guide.")]
    [RateLimit(TimeSeconds = 10, Global = true)]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public Task OzmaAsync() => DiscordUtilities.PostImage(_http, Context,
        "https://cdn.discordapp.com/attachments/588592729609469952/721522648949063730/unknown.png");

    [Command("accel", RunMode = RunMode.Async)]
    [Description("Shows the Acceleration Bomb guide.")]
    [RateLimit(TimeSeconds = 10, Global = true)]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public Task AccelBombAsync() => DiscordUtilities.PostImage(_http, Context,
        "https://cdn.discordapp.com/attachments/550709673104375818/721527266441560064/unknown.png");
}