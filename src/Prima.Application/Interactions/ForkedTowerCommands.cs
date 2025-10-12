using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Prima.DiscordNet;
using Prima.DiscordNet.Attributes;
using Prima.Game.FFXIV.FFLogs.Rules;
using Prima.Resources;
using Color = Discord.Color;

namespace Prima.Application.Interactions;

[Group("forked-tower", "Forked Tower run-related commands.")]
[ModuleScope(ModuleScopeAttribute.ModuleScoping.Guild, GuildId = SpecialGuilds.CrystalExploratoryMissions)]
public class ForkedTowerCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<ForkedTowerCommands> _logger;

    public ForkedTowerCommands(ILogger<ForkedTowerCommands> logger)
    {
        _logger = logger;
    }


    [SlashCommand("prog", "Check the progress points of other members (maximum of 25).")]
    public async Task ProgressPoints(
        // Need to have each user be separate for d.NET (max 25 parameters)
        SocketGuildUser user1,
        SocketGuildUser? user2 = null,
        SocketGuildUser? user3 = null,
        SocketGuildUser? user4 = null,
        SocketGuildUser? user5 = null,
        SocketGuildUser? user6 = null,
        SocketGuildUser? user7 = null,
        SocketGuildUser? user8 = null,
        SocketGuildUser? user9 = null,
        SocketGuildUser? user10 = null,
        SocketGuildUser? user11 = null,
        SocketGuildUser? user12 = null,
        SocketGuildUser? user13 = null,
        SocketGuildUser? user14 = null,
        SocketGuildUser? user15 = null,
        SocketGuildUser? user16 = null,
        SocketGuildUser? user17 = null,
        SocketGuildUser? user18 = null,
        SocketGuildUser? user19 = null,
        SocketGuildUser? user20 = null,
        SocketGuildUser? user21 = null,
        SocketGuildUser? user22 = null,
        SocketGuildUser? user23 = null,
        SocketGuildUser? user24 = null,
        SocketGuildUser? user25 = null)
    {
        // Convert to a sane representation
        var users = new List<SocketGuildUser?>
            {
                user1, user2, user3, user4, user5, user6, user7, user8, user9, user10, user11, user12, user13, user14,
                user15, user16, user17, user18, user19, user20, user21, user22, user23, user24, user25,
            }
            .Where(user => user != null)
            .Select(user => user!)
            .ToList();
        _logger.LogInformation("Checking Forked Tower progression roles for {UserCount} users", users.Count);

        // Group by highest progression role
        var usersByHighestRole = users
            .GroupBy(GetHighestProgRole)
            .Select(g => new EmbedFieldBuilder()
                .WithName(Context.Guild.GetRole(g.Key).Name)
                .WithValue(string.Join("\n", g.Select(u => u.DisplayName)))
                .WithIsInline(true))
            .ToList();

        var embed = new EmbedBuilder()
            .WithTitle("Forked Tower Progression")
            .WithDescription("Members grouped by progression:")
            .WithColor(new Color(0x00, 0x80, 0xFF))
            .WithFields(usersByHighestRole)
            .Build();

        await RespondAsync(embed: embed, ephemeral: true);
    }

    private static ulong GetHighestProgRole(IGuildUser member)
    {
        var roleOrder = new[]
        {
            ForkedTowerRules.ClearedForkedTower, ForkedTowerRules.MagitaurProgression,
            ForkedTowerRules.MarbleDragonProgression, ForkedTowerRules.DeadStarsProgression,
            ForkedTowerRules.DemonTabletProgression,
        };
        return roleOrder.FirstOrDefault(member.HasRole);
    }

    [Group("guide", "Guide macros")]
    public class GuideCommands : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("demon-markers", "Shows the Demon Tablet markers guide.")]
        public Task DemonTabletMarkers() => RespondAsync("https://i.imgur.com/oEa6Vo3.png");

        [SlashCommand("demon-meteors", "Shows the Demon Tablet meteors guide.")]
        public Task DemonTabletMeteors() => RespondAsync("https://i.imgur.com/K0HMR3K.png");

        [SlashCommand("hallway-1-traps", "Shows the first hallway traps guide.")]
        public Task Hallway1Traps() => RespondAsync("https://i.imgur.com/eYcyC2l.png");

        [SlashCommand("dead-stars-markers", "Shows the Dead Stars markers guide.")]
        public Task DeadStarsMarkers() => RespondAsync("https://i.imgur.com/4lQ43Gw.png");

        [SlashCommand("healer-wings", "Shows the Dead Stars Healer Wings guide.")]
        public Task HealerWingsMarkers() => RespondAsync(embed: new EmbedBuilder()
            .WithTitle("Boss 2: Alternative Fire Soak Setup Positions \"Healer Wings\"")
            .WithDescription(
                "The thought process is to remove the confusion of Left/Right stacking and instead just have Healers always be Left/Right and DPS always be Out. Use the lines on the floor to know where to stand for both groups.\nCredit to Aether Group 2 for the design idea.")
            .WithImageUrl("https://i.imgur.com/XEMn7Pj.png")
            .Build());

        [SlashCommand("dead-stars-enrage", "Shows the Dead Stars enrage guide.")]
        public Task DeadStarsEnrageMarkers() => RespondAsync("https://i.imgur.com/OaomQ9x.png");

        [SlashCommand("dragon-markers", "Shows the Marble Dragon markers guide.")]
        public Task MarbleDragonMarkers() => RespondAsync("https://i.imgur.com/qHnzfMC.jpeg");

        [SlashCommand("lockward-traps", "Shows the Lockward traps guide.")]
        public Task LockwardTraps() => RespondAsync("https://i.imgur.com/e0xulg2.png");

        [SlashCommand("lockward-cheat-sheet", "Shows the Lockward cheat sheet.")]
        public Task LockwardCheatSheet() => RespondAsync("https://i.imgur.com/tcYJ5Co.jpeg");

        [SlashCommand("magitaur-markers", "Shows the Magitaur markers guide.")]
        public Task MagitaurMarkers() => RespondAsync("https://i.imgur.com/oljHU1i.jpeg");

        [SlashCommand("bridges", "Shows the bridges guide.")]
        public Task BridgeMarkers() => RespondAsync("https://i.imgur.com/OHgehU1.png");
    }
}