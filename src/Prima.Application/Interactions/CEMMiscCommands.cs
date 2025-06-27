using Discord.Interactions;
using Prima.DiscordNet.Attributes;
using Prima.Resources;

namespace Prima.Application.Interactions;

[ModuleScope(ModuleScopeAttribute.ModuleScoping.Guild, GuildId = SpecialGuilds.CrystalExploratoryMissions)]
public class CEMMiscCommands : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly List<string> OopsImages = new()
    {
        "https://cdn.discordapp.com/attachments/327524883095617546/1381371836507619338/the_incident.gif?ex=685080c6&is=684f2f46&hm=9229d0f1918b2d587448151edcc5b1186f07c7360dd0fdd98f8db184b931e833&",
        "https://cdn.discordapp.com/attachments/1379517980987363470/1383475310435106846/FINAL_FANTASY_XIV_2025-06-14_10-56-15_-_Trim_-_Trim.gif?ex=68503ec9&is=684eed49&hm=4a1f8eea2638f420536ec17791f5bbace78ab6ede91e71c4427c7d3d1ab6518b&",
    };

    [SlashCommand("oops", "oops")]
    public Task Oops() => RespondAsync(string.Join("\n", OopsImages));
}