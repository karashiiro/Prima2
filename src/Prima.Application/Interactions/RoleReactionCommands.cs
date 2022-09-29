using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Prima.Data;
using Prima.DiscordNet.Attributes;

namespace Prima.Application.Interactions;

[Group("role-reactions", "Manage role reactions for this guild.")]
[ModuleScope(ModuleScopeAttribute.ModuleScoping.Global)]
public class RoleReactionCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IRoleReactionsDb _db;
    private readonly ILogger<RoleReactionCommands> _logger;

    public RoleReactionCommands(IRoleReactionsDb db, ILogger<RoleReactionCommands> logger)
    {
        _db = db;
        _logger = logger;
    }

    [SlashCommand("list", "Retrieve the list of role reactions for this guild.")]
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task ListRoleReactions()
    {
        try
        {
            var roleReactions = await _db.GetRoleReactions(Context.Guild.Id);
            if (!roleReactions.Any())
            {
                await RespondAsync("No role reactions are registered for this guild.");
                return;
            }

            // Generate list of role reactions in an embed
            var embed = new EmbedBuilder()
                .WithTitle($"{Context.Guild.Name} Role Reactions")
                .WithColor(52, 152, 219)
                .WithDescription(roleReactions.Aggregate("",
                    (agg, next) => agg + $"<#{next.ChannelId}> <:e:{next.EmojiId}>: <@&{next.RoleId}>\n"))
                .Build();
            await RespondAsync(embed: embed);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve role reactions for guild {GuildName} (id={GuildId})",
                Context.Guild.Name, Context.Guild.Id);
            await RespondAsync("Failed to retrieve role reactions for this guild.");
        }
    }

    [SlashCommand("add", "Add a role reaction.")]
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task AddRoleReaction(
        [Summary(description: "The channel to add the role reaction to.")]
        ITextChannel channel,
        [Summary(description: "The ID of the emoji to add a reaction with.")]
        string emojiId,
        [Summary(description: "The role to add a reaction for.")]
        IRole role)
    {
        try
        {
            var roleReaction = GetBasicInfo(channel, emojiId, role);
            await _db.CreateRoleReaction(roleReaction);
            _logger.LogInformation(
                "Role reaction for role {RoleName} added to channel {ChannelName} by user {DiscordName}",
                role.Name, channel.Name, Context.User.ToString());
            await RespondAsync("Role reaction added.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to add role reaction");
            await RespondAsync("Failed to add role reaction.");
        }
    }

    [SlashCommand("remove", "Remove a role reaction.")]
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task RemoveRoleReaction(
        [Summary(description: "The channel to remove the role reaction from.")]
        ITextChannel channel,
        [Summary(description: "The role to remove a reaction for.")]
        IRole role)
    {
        try
        {
            var roleReaction = GetBasicInfo(channel, null, role);
            if (!await _db.RemoveRoleReaction(roleReaction))
            {
                await RespondAsync("Role reaction not found.");
                return;
            }

            _logger.LogInformation(
                "Role reaction for role {RoleName} removed from channel {ChannelName} by user {DiscordName}",
                role.Name, channel.Name, Context.User.ToString());
            await RespondAsync("Role reaction removed.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to remove role reaction");
            await RespondAsync("Failed to remove role reaction.");
        }
    }

    [SlashCommand("set-eureka", "Sets a registered role reaction to be used as the Eureka special role.")]
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task SetEurekaRoleReaction(
        [Summary(description: "The channel the existing role reaction is in.")]
        ITextChannel channel,
        [Summary(description: "The role ID of the role.")]
        IRole role)
    {
        try
        {
            var roleReaction = GetBasicInfo(channel, null, role);
            roleReaction.Eureka = true;
            if (await _db.UpdateRoleReaction(roleReaction) == null)
            {
                await RespondAsync("Role reaction not found.");
                return;
            }

            _logger.LogInformation(
                "Eureka flag set for role reaction with role {RoleName} in channel {ChannelName} by user {DiscordName}",
                role.Name, channel.Name, Context.User.ToString());
            await RespondAsync("Role reaction set to Eureka role.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to set Eureka flag on role");
            await RespondAsync("Failed to set Eureka flag on role.");
        }
    }

    [SlashCommand("unset-eureka", "Unsets a registered role reaction as the Eureka special role.")]
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task UnsetEurekaRoleReaction(
        [Summary(description: "The channel the existing role reaction is in.")]
        ITextChannel channel,
        [Summary(description: "The role ID of the role.")]
        IRole role)
    {
        try
        {
            var roleReaction = GetBasicInfo(channel, null, role);
            roleReaction.Eureka = false;
            await _db.UpdateRoleReaction(roleReaction);
            if (await _db.UpdateRoleReaction(roleReaction) == null)
            {
                await RespondAsync("Role reaction not found.");
                return;
            }

            _logger.LogInformation(
                "Eureka flag unset for role reaction with role {RoleName} in channel {ChannelName} by user {DiscordName}",
                role.Name, channel.Name, Context.User.ToString());
            await RespondAsync("Role reaction unset as Eureka role.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to unset Eureka flag on role");
            await RespondAsync("Failed to unset Eureka flag on role.");
        }
    }

    [SlashCommand("set-bozja", "Sets a registered role reaction to be used as the Bozja special role.")]
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task SetBozjaRoleReaction(
        [Summary(description: "The channel the existing role reaction is in.")]
        ITextChannel channel,
        [Summary(description: "The role ID of the role.")]
        IRole role)
    {
        try
        {
            var roleReaction = GetBasicInfo(channel, null, role);
            roleReaction.Bozja = true;
            await _db.UpdateRoleReaction(roleReaction);
            if (await _db.UpdateRoleReaction(roleReaction) == null)
            {
                await RespondAsync("Role reaction not found.");
                return;
            }

            _logger.LogInformation(
                "Bozja flag set for role reaction with role {RoleName} in channel {ChannelName} by user {DiscordName}",
                role.Name, channel.Name, Context.User.ToString());
            await RespondAsync("Role reaction set to Bozja role.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to set Bozja flag on role");
            await RespondAsync("Failed to set Bozja flag on role.");
        }
    }

    [SlashCommand("unset-bozja", "Unsets a registered role reaction as the Bozja special role.")]
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task UnsetBozjaRoleReaction(
        [Summary(description: "The channel the existing role reaction is in.")]
        ITextChannel channel,
        [Summary(description: "The role ID of the role.")]
        IRole role)
    {
        try
        {
            var roleReaction = GetBasicInfo(channel, null, role);
            roleReaction.Bozja = false;
            await _db.UpdateRoleReaction(roleReaction);
            if (await _db.UpdateRoleReaction(roleReaction) == null)
            {
                await RespondAsync("Role reaction not found.");
                return;
            }

            _logger.LogInformation(
                "Bozja flag unset for role reaction with role {RoleName} in channel {ChannelName} by user {DiscordName}",
                role.Name, channel.Name, Context.User.ToString());
            await RespondAsync("Role reaction unset as Bozja role.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to unset Bozja flag on role");
            await RespondAsync("Failed to unset Bozja flag on role.");
        }
    }

    private static RoleReactionInfo GetBasicInfo(IGuildChannel channel, string? emojiId, IRole role)
    {
        return new RoleReactionInfo
        {
            GuildId = channel.GuildId.ToString(),
            ChannelId = channel.Id.ToString(),
            EmojiId = emojiId,
            RoleId = role.Id.ToString(),
            Eureka = null,
            Bozja = null,
        };
    }
}