using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Prima.Attributes;
using Prima.Resources;
using Color = Discord.Color;

namespace Prima.Stable.Modules
{
    [Name("Castrum Extra Module")]
    public class CastrumExtraModule : ModuleBase<SocketCommandContext>
    {
        public CommandService CommandManager { get; set; }
        public HttpClient Http { get; set; }
        public IServiceProvider Services { get; set; }

        [Command("bozhelp", RunMode = RunMode.Async)]
        [Description("Shows help information for the extra Bozja commands.")]
        [RateLimit(TimeSeconds = 10, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task BozjaHelpAsync()
        {
            var commands = (await CommandManager.GetExecutableCommandsAsync(Context, Services))
                .Where(command => command.Attributes.Any(attr => attr is DescriptionAttribute))
                .Where(command => command.Module.Name == "Castrum Extra Module")
                .Where(command => command.Name != "bozhelp");

            var embed = new EmbedBuilder()
                .WithTitle("Useful Commands (Bozja)")
                .WithColor(Color.LightOrange)
                .WithDescription(commands
                    .Select(c =>
                    {
                        var descAttr = (DescriptionAttribute)c.Attributes.First(attr => attr is DescriptionAttribute);
                        return $"`~{c.Name}` - {descAttr.Description}\n";
                    })
                    .Aggregate((text, next) => text + next))
                .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("star", RunMode = RunMode.Async)]
        [Description("Shows the Bozjan Southern Front star mob guide.")]
        [RateLimit(TimeSeconds = 1, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task StarMobsAsync() => Util.PostImage(Http, Context, "https://i.imgur.com/muvBR1Z.png");

        [Command("cluster", RunMode = RunMode.Async)]
        [Description("Shows the Bozjan Southern Front cluster path guide.")]
        [RateLimit(TimeSeconds = 1, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task BozjaClustersAsync() => Util.PostImage(Http, Context, "https://i.imgur.com/WANkcVe.jpeg");
    }
}