using Discord.Commands;
using Prima.DiscordNet;
using Prima.DiscordNet.Attributes;
using Prima.Resources;

namespace Prima.Application.Commands.PSO2;

[Name("PSO2 Guides")]
public class GuideCommands : ModuleBase<SocketCommandContext>
{
    private readonly HttpClient _http;

    public GuideCommands(HttpClient http)
    {
        _http = http;
    }

    [Command("aeriomats")]
    [Description("Shows the Aerio materials route.")]
    [RestrictFromGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public Task AerioMaterials()
        => DiscordUtilities.PostImage(_http, Context, "https://i.imgur.com/I8J001K.png");
}