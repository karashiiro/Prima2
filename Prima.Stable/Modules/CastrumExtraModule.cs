using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using Prima.Attributes;
using Prima.Resources;

namespace Prima.Stable.Modules
{
    [Name("Castrum Extra Module")]
    public class CastrumExtraModule : ModuleBase<SocketCommandContext>
    {
        public HttpClient Http { get; set; }

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