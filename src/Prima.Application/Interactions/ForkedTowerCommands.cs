using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Prima.DiscordNet;
using Prima.DiscordNet.Attributes;
using Prima.Game.FFXIV.FFLogs.Rules;

namespace Prima.Application.Interactions;

[Group("forked-tower", "Forked Tower run-related commands.")]
[ModuleScope(ModuleScopeAttribute.ModuleScoping.Global)]
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

        [SlashCommand("dead-stars-markers", "Shows the Dead Stars markers guide.")]
        public Task DeadStarsMarkers() => RespondAsync("https://i.imgur.com/4lQ43Gw.png");

        [SlashCommand("dragon-markers", "Shows the Marble Dragon markers guide.")]
        public Task MarbleDragonMarkers() => RespondAsync("https://i.imgur.com/qHnzfMC.jpeg");

        [SlashCommand("magitaur-markers", "Shows the Magitaur markers guide.")]
        public Task MagitaurMarkers() => RespondAsync("https://i.imgur.com/oljHU1i.jpeg");

        [SlashCommand("bridges", "Shows the bridges guide.")]
        public Task BridgeMarkers() => RespondAsync("https://i.imgur.com/dAiTIrk.png");
    }
}