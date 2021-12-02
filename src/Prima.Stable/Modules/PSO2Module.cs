using Discord.Commands;
using System.Net.Http;
using System.Threading.Tasks;
using Prima.DiscordNet;
using Prima.DiscordNet.Attributes;
using Prima.Resources;

namespace Prima.Stable.Modules
{
    [Name("PSO2 Module")]
    public class PSO2Module : ModuleBase<SocketCommandContext>
    {
        public HttpClient Http { get; set; }

        [Command("aeriomats")]
        [Description("Shows the Aerio materials route.")]
        [RestrictFromGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task AerioMaterials()
            => DiscordUtilities.PostImage(Http, Context, "https://i.imgur.com/I8J001K.png");
    }
}
