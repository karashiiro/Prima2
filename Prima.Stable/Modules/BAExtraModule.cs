using Discord;
using Discord.Commands;
using Prima.DiscordNet;
using Prima.DiscordNet.Attributes;
using Prima.Resources;
using Prima.Services;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Color = Discord.Color;

namespace Prima.Stable.Modules
{
    [Name("BA Extra Module")]
    public class BAExtraModule : ModuleBase<SocketCommandContext>
    {
        public IDbService Db { get; set; }
        public HttpClient Http { get; set; }

        [Command("bahelp", RunMode = RunMode.Async)]
        [Description("Shows help information for the extra BA commands.")]
        [RateLimit(TimeSeconds = 10, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task BAHelpAsync()
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            var prefix = Db.Config.Prefix.ToString();
            if (guildConfig != null && guildConfig.Prefix != ' ')
                prefix = guildConfig.Prefix.ToString();

            var baseCommands = await DiscordUtilities.GetFormattedCommandList(typeof(BAExtraModule), prefix, except: new List<string> { "bahelp" });
            var hostCommands = await DiscordUtilities.GetFormattedCommandList(typeof(RunModule), prefix);

            var embed = new EmbedBuilder()
                .WithTitle("Useful Commands (Baldesion Arsenal)")
                .WithColor(Color.LightOrange)
                .WithDescription(baseCommands + "==============================\n" + hostCommands)
                .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("lfgcountsba", RunMode = RunMode.Async)]
        [Description("Get the LFG role counts of all guild members for the Baldesion Arsenal.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task GetBAProgressionCounts()
        {
            var members = Context.Guild.Users;

            var lfgFragMembers = members.Where(m => m.HasRole(551619522340323328));
            var lfgAvOzmaMembers = members.Where(m => m.HasRole(551619527008321557));
            var lfgOzmaClearMembers = members.Where(m => m.HasRole(551619529336422402));
            var lfgBaAllMembers = members.Where(m => m.HasRole(574696475976794175));

            const string outFormat = "LFG Counts (includes overlap):\n" +
                                     "LFG Frags/Fresh Prog: {0}\n" +
                                     "LFG AV/Ozma Prog: {1}\n" +
                                     "LFG Ozma Clears/Farms: {2}\n" +
                                     "LFG BA All: {3}";
            return ReplyAsync(string.Format(
                outFormat,
                lfgFragMembers.Count(),
                lfgAvOzmaMembers.Count(),
                lfgOzmaClearMembers.Count(),
                lfgBaAllMembers.Count()));
        }

        [Command("portals", RunMode = RunMode.Async)]
        [Description("Shows the portal map.")]
        [RateLimit(TimeSeconds = 10, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task PortalsAsync() => DiscordUtilities.PostImage(Http, Context, "https://i.imgur.com/XcXACQp.png");

        [Command("owain", RunMode = RunMode.Async)]
        [Description("Shows the Owain guide.")]
        [RateLimit(TimeSeconds = 10, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task OwainAsync() => DiscordUtilities.PostImage(Http, Context, "https://i.imgur.com/ADwopqC.jpg");

        [Command("art", RunMode = RunMode.Async)]
        [Description("Shows the Art guide.")]
        [RateLimit(TimeSeconds = 10, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task ArtAsync() => DiscordUtilities.PostImage(Http, Context, "https://i.imgur.com/sehs4Tw.jpg");

        [Command("raiden", RunMode = RunMode.Async)]
        [Description("Shows the Raiden guide.")]
        [RateLimit(TimeSeconds = 10, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task RaidenAsync() => DiscordUtilities.PostImage(Http, Context, "https://cdn.discordapp.com/attachments/588592729609469952/721522493197779035/unknown.png");

        [Command("av", RunMode = RunMode.Async)]
        [Description("Shows the Absolute Virtue guide.")]
        [RateLimit(TimeSeconds = 10, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task AbsoluteVirtueAsync() => DiscordUtilities.PostImage(Http, Context, "https://cdn.discordapp.com/attachments/588592729609469952/721522585866731580/unknown.png");

        [Command("ozma", RunMode = RunMode.Async)]
        [Description("Shows the Ozma guide.")]
        [RateLimit(TimeSeconds = 10, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task OzmaAsync() => DiscordUtilities.PostImage(Http, Context, "https://cdn.discordapp.com/attachments/588592729609469952/721522648949063730/unknown.png");

        [Command("accel", RunMode = RunMode.Async)]
        [Description("Shows the Acceleration Bomb guide.")]
        [RateLimit(TimeSeconds = 10, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task AccelBombAsync() => DiscordUtilities.PostImage(Http, Context, "https://cdn.discordapp.com/attachments/550709673104375818/721527266441560064/unknown.png");
    }
}