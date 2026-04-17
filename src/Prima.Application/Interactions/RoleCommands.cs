using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Prima.DiscordNet;
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

    [SlashCommand("set-main-vanity-role", "Sets your primary vanity role in this server. Temporarily removes all other vanity roles.")]
    public async Task SetMainVanityRole(IRole role)
    {
        var guild = Context.Guild;
        if (guild == null)
        {
            await RespondAsync("This command can only be used in a guild.");
            return;
        }

        var user = await Context.Client.Rest.GetGuildUserAsync(guild.Id, Context.User.Id);
        
        var allGuildVanityRoles = SpecialGuildVanityRoles.GetRoles(user.GuildId);
        if (!allGuildVanityRoles.Contains(role.Id))
        {
            await RespondAsync("That role is not a valid vanity role.", ephemeral: true);
            return;       
        }

        _logger.LogInformation("Setting main vanity role for {User} to {Role}", Context.User.Username, role.Name);

        // Register any untracked vanity roles
        await user.UpdateVanityRoles(_db, _logger);
        
        var dbUser = await _db.GetUserByDiscordId(user.Id);
        if (dbUser == null)
        {
            await RespondAsync("You are not currently registered. Please register yourself with `~iam`.",
                ephemeral: true);
            return;
        }

        // Get the vanity roles the user has, according to the guild
        var roleIds = dbUser.GetVanityRoles(guild.Id);
        _logger.LogInformation("Found {RoleCount} vanity roles for {User}", roleIds.Count, Context.User.Username);

        if (!roleIds.Contains(role.Id))
        {
            await RespondAsync("You don't have that role! Check your vanity roles with /list-vanity-roles.",
                ephemeral: true);
            return;
        }

        // Remove all vanity roles except the main one
        foreach (var roleId in roleIds)
        {
            if (roleId == role.Id) continue;
            await user.RemoveRoleAsync(roleId);
        }
        await user.AddRoleAsync(role.Id);

        await RespondAsync($"Updated main vanity role to {role.Name}.", ephemeral: true);
    }

    [SlashCommand("restore-vanity-roles", "Restores your vanity roles to their default state.")]
    public async Task RestoreVanityRoles()
    {
        var guild = Context.Guild;
        if (guild == null)
        {
            await RespondAsync("This command can only be used in a guild.");
            return;
        }

        _logger.LogInformation("Restoring vanity roles for {User}", Context.User.Username);

        var user = await Context.Client.Rest.GetGuildUserAsync(guild.Id, Context.User.Id);

        // Register any untracked vanity roles
        await user.UpdateVanityRoles(_db, _logger);
        
        var dbUser = await _db.GetUserByDiscordId(user.Id);
        if (dbUser == null)
        {
            await RespondAsync("You are not currently registered. Please register yourself with `~iam`.",
                ephemeral: true);
            return;
        }

        // Get the vanity roles the user has
        var roleIds = dbUser.GetVanityRoles(guild.Id);
        _logger.LogInformation("Found {RoleCount} vanity roles for {User}", roleIds.Count, Context.User.Username);

        // Restore all vanity roles
        foreach (var roleId in roleIds)
        {
            await user.AddRoleAsync(roleId);
        }

        await RespondAsync("Restored all vanity roles.", ephemeral: true);
    }

    [SlashCommand("list-vanity-roles", "Check your vanity roles on this server.")]
    public async Task ListVanityRoles()
    {
        var guild = Context.Guild;
        if (guild == null)
        {
            await RespondAsync("This command can only be used in a guild.");
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
        await user.UpdateVanityRoles(_db, _logger);

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
}