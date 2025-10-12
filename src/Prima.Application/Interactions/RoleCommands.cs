using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Prima.DiscordNet.Attributes;
using Prima.Resources;
using Prima.Services;

namespace Prima.Application.Interactions;

[ModuleScope(ModuleScopeAttribute.ModuleScoping.Global)]
public class RoleCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<RoleCommands> _logger;
    private readonly IDbService _db;

    public RoleCommands(ILogger<RoleCommands> logger, IDbService db)
    {
        _logger = logger;
        _db = db;
    }

    [SlashCommand("list-vanity-roles", "Check your vanity roles on this server.")]
    public async Task ListVanityRoles()
    {
        var guild = Context.Guild;
        if (guild == null)
        {
            await ReplyAsync("This command can only be used in a guild.");
            return;
        }

        _logger.LogInformation("Listing vanity roles for {User}", Context.User.Username);

        var user = await Context.Client.Rest.GetGuildUserAsync(guild.Id, Context.User.Id);
        var dbUser = await _db.GetUserByDiscordId(user.Id);
        if (dbUser == null)
        {
            await RespondAsync("You are not currently registered. Please register yourself with `~iam`.",
                ephemeral: true);
            return;
        }

        // Register any untracked vanity roles
        await UpdateVanityRoles(user);

        // Get the vanity roles the user has, according to the guild
        var roleIds = dbUser.GetVanityRoles(guild.Id);
        _logger.LogInformation("Found {RoleCount} vanity roles for {User}", roleIds.Count, Context.User.Username);

        var restGuild = await Context.Client.Rest.GetGuildAsync(guild.Id);
        var roles = roleIds
            .Select(r => restGuild.GetRole(r))
            .Where(r => r != null)
            .ToList();

        // Return list of roles to user
        await RespondAsync(
            embed: new EmbedBuilder()
                .WithTitle("Your vanity roles")
                .WithDescription(string.Join("\n", roles.Select(r => r.Mention)))
                .Build(),
            ephemeral: true);
    }

    private async Task UpdateVanityRoles(IGuildUser user)
    {
        var dbUser = await _db.GetUserByDiscordId(user.Id);
        if (dbUser == null)
        {
            return;
        }

        var allGuildVanityRoles = SpecialGuildVanityRoles.GetRoles(user.GuildId);
        var vanityRoles = user.RoleIds.Where(roleId => allGuildVanityRoles.Contains(roleId)).ToList();

        dbUser.AddVanityRoles(user.GuildId, vanityRoles);

        _logger.LogInformation("Updating vanity roles for {User}", user.Username);
        await _db.UpdateUser(dbUser);
    }
}