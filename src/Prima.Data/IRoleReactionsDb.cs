namespace Prima.Data;

public interface IRoleReactionsDb
{
    Task<IList<RoleReactionInfo>> GetRoleReactions(ulong guildId);

    Task<RoleReactionInfo?> GetRoleReaction(ulong guildId, ulong channelId, ulong emoteId);

    Task CreateRoleReaction(RoleReactionInfo rrInfo);

    Task<bool> RemoveRoleReaction(RoleReactionInfo rrInfo);

    Task<RoleReactionInfo?> UpdateRoleReaction(RoleReactionInfo rrInfo);
}