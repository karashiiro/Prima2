using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using Prima.Attributes;
using Prima.Resources;

namespace Prima.Stable.Modules
{
    public class BAExtraModule : ModuleBase<SocketCommandContext>
    {
        public HttpClient Http { get; set; }

        [Command("portals", RunMode = RunMode.Async)]
        [Description("Shows the portal map.")]
        [RateLimit(TimeSeconds = 30, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task PortalsAsync() => Util.PostImage(Http, Context, "https://i.imgur.com/XcXACQp.png");

        [Command("owain", RunMode = RunMode.Async)]
        [Description("Shows the Owain guide.")]
        [RateLimit(TimeSeconds = 120, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task OwainAsync() => Util.PostImage(Http, Context, "https://i.imgur.com/ADwopqC.jpg");

        [Command("art", RunMode = RunMode.Async)]
        [Description("Shows the Art guide.")]
        [RateLimit(TimeSeconds = 120, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task ArtAsync() => Util.PostImage(Http, Context, "https://i.imgur.com/sehs4Tw.jpg");

        [Command("raiden", RunMode = RunMode.Async)]
        [Description("Shows the Raiden guide.")]
        [RateLimit(TimeSeconds = 120, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task RaidenAsync() => Util.PostImage(Http, Context, "https://cdn.discordapp.com/attachments/588592729609469952/721522493197779035/unknown.png");

        [Command("av", RunMode = RunMode.Async)]
        [Description("Shows the Absolute Virtue guide.")]
        [RateLimit(TimeSeconds = 120, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task AbsoluteVirtueAsync() => Util.PostImage(Http, Context, "https://cdn.discordapp.com/attachments/588592729609469952/721522585866731580/unknown.png");

        [Command("ozma", RunMode = RunMode.Async)]
        [Description("Shows the Ozma guide.")]
        [RateLimit(TimeSeconds = 120, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task OzmaAsync() => Util.PostImage(Http, Context, "https://cdn.discordapp.com/attachments/588592729609469952/721522648949063730/unknown.png");

        [Command("accel", RunMode = RunMode.Async)]
        [Description("Shows the Acceleration Bomb guide.")]
        [RateLimit(TimeSeconds = 120, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task AccelBombAsync() => Util.PostImage(Http, Context, "https://cdn.discordapp.com/attachments/550709673104375818/721527266441560064/unknown.png");
    }
}