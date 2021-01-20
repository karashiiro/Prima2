using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Prima.Attributes;
using Prima.Resources;
using Prima.Services;
using Color = Discord.Color;

namespace Prima.Stable.Modules
{
    [Name("Castrum Extra Module")]
    public class CastrumExtraModule : ModuleBase<SocketCommandContext>
    {
        public DbService Db { get; set; }
        public HttpClient Http { get; set; }
        public IServiceProvider Services { get; set; }

        [Command("bozhelp", RunMode = RunMode.Async)]
        [Description("Shows help information for the extra Bozja commands.")]
        [RateLimit(TimeSeconds = 10, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task BozjaHelpAsync()
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            var prefix = Db.Config.Prefix.ToString();
            if (guildConfig != null && guildConfig.Prefix != ' ')
                prefix = guildConfig.Prefix.ToString();

            var commands = await DiscordUtilities.GetFormattedCommandList(Services, Context, prefix,
                "Castrum Extra Module", except: new List<string> {"bozhelp"});

            var embed = new EmbedBuilder()
                .WithTitle("Useful Commands (Bozja)")
                .WithColor(Color.LightOrange)
                .WithDescription(commands)
                .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("star", RunMode = RunMode.Async)]
        [Description("Shows the Bozjan Southern Front star mob guide.")]
        [RateLimit(TimeSeconds = 1, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task StarMobsAsync() => DiscordUtilities.PostImage(Http, Context, "https://i.imgur.com/muvBR1Z.png");

        [Command("cluster", RunMode = RunMode.Async)]
        [Description("Shows the Bozjan Southern Front cluster path guide.")]
        [RateLimit(TimeSeconds = 1, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task BozjaClustersAsync() => DiscordUtilities.PostImage(Http, Context, "https://i.imgur.com/WANkcVe.jpeg");
    }
}