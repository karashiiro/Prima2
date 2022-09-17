using Discord;
using Discord.Commands;
using Prima.Application.Commands.FFXIV.DelubrumReginae;
using Prima.DiscordNet;
using Prima.DiscordNet.Attributes;
using Prima.Resources;
using Prima.Services;

namespace Prima.Application.Commands.FFXIV.Bozja;

[Name("Bozja Info")]
public class BozjaInfoCommands : ModuleBase<SocketCommandContext>
{
    private readonly IDbService _db;
    private readonly HttpClient _http;

    public BozjaInfoCommands(IDbService db, HttpClient http)
    {
        _db = db;
        _http = http;
    }

    [Command("bozhelp", RunMode = RunMode.Async)]
    [Description("Shows help information for the extra Bozja commands.")]
    [RateLimit(TimeSeconds = 10, Global = true)]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public async Task BozjaHelpAsync()
    {
        var guildConfig = _db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
        var prefix = _db.Config.Prefix.ToString();
        if (guildConfig != null && guildConfig.Prefix != ' ')
            prefix = guildConfig.Prefix.ToString();

        var commands = DiscordUtilities.GetFormattedCommandList(
            typeof(BozjaInfoCommands),
            prefix,
            except: new List<string> { "bozhelp" });
        commands += DiscordUtilities.GetFormattedCommandList(typeof(DRInfoCommands), prefix);
        commands += DiscordUtilities.GetFormattedCommandList(typeof(DRRunCommands), prefix);

        var embed = new EmbedBuilder()
            .WithTitle("Useful Commands (Bozja)")
            .WithColor(Discord.Color.LightOrange)
            .WithDescription(commands)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("lostactions", RunMode = RunMode.Async)]
    [Description("Shows the Lost Action guide images.")]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public Task LostActionsAsync()
    {
        return ReplyAsync(embed: new EmbedBuilder()
            .WithTitle("Lost Actions commands")
            .WithDescription("~bozjakit - General use Bozja/Zadnor loadout guide\n" +
                             "~drnspeedrun - Delubrum Reginae (Normal) loadout guide\n" +
                             "~drskit - Delubrum Reginae (Savage) loadout guide")
            .WithColor(Discord.Color.DarkOrange)
            .Build());
    }

    [Command("bozjakit", RunMode = RunMode.Async)]
    [Description("Shows the Bozja/Zadnor Lost Actions loadout guide.")]
    [RateLimit(TimeSeconds = 10, Global = true)]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public Task BozjaKitAsync() => DiscordUtilities.PostImage(_http, Context, "https://i.imgur.com/9aZJDkm.png");

    [Command("star", RunMode = RunMode.Async)]
    [Description("Shows the Bozjan Southern Front star mob guide.")]
    [RateLimit(TimeSeconds = 10, Global = true)]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public Task StarMobsAsync() => DiscordUtilities.PostImage(_http, Context, "https://i.imgur.com/muvBR1Z.png");

    [Command("cluster", RunMode = RunMode.Async)]
    [Alias("clusters")]
    [Description("Shows the Bozjan Southern Front cluster path guide.")]
    [RateLimit(TimeSeconds = 10, Global = true)]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public Task BozjaClustersAsync() => DiscordUtilities.PostImage(_http, Context, "https://i.imgur.com/FuG4wDK.png");

    [Command("memories", RunMode = RunMode.Async)]
    [Description("Shows the Bozjan Southern Front memory path guide.")]
    [RateLimit(TimeSeconds = 10, Global = true)]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public Task BozjaMemoriesAsync() => DiscordUtilities.PostImage(_http, Context, "https://i.imgur.com/JSCoxi8.png");
}