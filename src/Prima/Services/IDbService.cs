using Prima.Game.FFXIV;
using Prima.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prima.Services
{
    public interface IDbService
    {
        GlobalConfiguration Config { get; }

        IEnumerable<DiscordGuildConfiguration> Guilds { get; }
        IEnumerable<DiscordXIVUser> Users { get; }
        IEnumerable<CachedMessage> CachedMessages { get; }
        IEnumerable<ChannelDescription> ChannelDescriptions { get; }
        IAsyncEnumerable<EventReaction> EventReactions { get; }
        IAsyncEnumerable<TimedRole> TimedRoles { get; }
        IAsyncEnumerable<Vote> Votes { get; }
        IAsyncEnumerable<VoteHost> VoteHosts { get; }
        IAsyncEnumerable<EphemeralPin> EphemeralPins { get; }

        Task<DiscordXIVUser?> GetUserByDiscordId(ulong discordId);

        Task<DiscordXIVUser?> GetUserByCharacterInfo(string? world, string? characterName);

        Task SetGlobalConfigurationProperty(string key, string value);

        Task SetGuildConfigurationProperty<T>(ulong guildId, string key, T value);

        Task AddGuild(DiscordGuildConfiguration config);

        Task<bool> AddEphemeralPin(ulong guildId, ulong channelId, ulong messageId, ulong pinnerRoleId, ulong pinnerId, DateTime pinTime);

        Task<bool> RemoveEphemeralPin(ulong messageId);

        Task<bool> AddVoteHost(ulong messageId, ulong ownerId);

        Task<bool> RemoveVoteHost(ulong messageId);

        Task<bool> AddVote(ulong messageId, ulong userId, string reactionName);

        Task<bool> RemoveVote(ulong messageId, ulong userId);

        Task<bool> AddEventReaction(ulong eventId, ulong userId);

        Task UpdateEventReaction(EventReaction updated);

        Task<bool> RemoveEventReaction(ulong eventId, ulong userId);

        Task RemoveAllEventReactions(ulong eventId);

        Task AddTimedRole(ulong roleId, ulong guildId, ulong userId, DateTime removalTime);

        Task RemoveTimedRole(ulong roleId, ulong userId);

        Task ConfigureRole(ulong guildId, string roleName, ulong roleId);

        Task DeconfigureRole(ulong guildId, string roleName);

        Task ConfigureRoleEmote(ulong guildId, ulong roleId, string emoteId);

        Task DeconfigureRoleEmote(ulong guildId, string emoteId);

        Task AddGuildTextDenylistEntry(ulong guildId, string regexString);

        Task RemoveGuildTextDenylistEntry(ulong guildId, string regexString);

        Task AddGuildTextGreylistEntry(ulong guildId, string regexString);

        Task RemoveGuildTextGreylistEntry(ulong guildId, string regexString);

        Task AddUser(DiscordXIVUser user);

        Task UpdateUser(DiscordXIVUser user);

        Task<bool> RemoveUser(string world, string name);

        Task<bool> RemoveUser(ulong lodestoneId);

        Task RemoveBrokenUsers();

        Task CacheMessage(CachedMessage message);

        Task DeleteMessage(ulong messageId);

        Task UpdateCachedMessage(CachedMessage message);

        Task AddChannelDescription(ulong channelId, string description);

        Task DeleteChannelDescription(ulong channelId);
    }
}