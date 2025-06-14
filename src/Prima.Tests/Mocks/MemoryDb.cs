using Prima.Extensions;
using Prima.Game.FFXIV;
using Prima.Models;
using Prima.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prima.Tests.Mocks
{
    public class MemoryDb : IDbService
    {
        public GlobalConfiguration Config { get; }

        public IEnumerable<DiscordGuildConfiguration> Guilds => _guilds;
        public IEnumerable<DiscordXIVUser> Users => _users;
        public IEnumerable<CachedMessage> CachedMessages => _cachedMessages;
        public IEnumerable<ChannelDescription> ChannelDescriptions => _channelDescriptions;
        public IAsyncEnumerable<EventReaction> EventReactions => _eventReactions.ToAsyncEnumerable();
        public IAsyncEnumerable<TimedRole> TimedRoles => _timedRoles.ToAsyncEnumerable();
        public IAsyncEnumerable<Vote> Votes => _votes.ToAsyncEnumerable();
        public IAsyncEnumerable<VoteHost> VoteHosts => _voteHosts.ToAsyncEnumerable();
        public IAsyncEnumerable<EphemeralPin> EphemeralPins => _ephemeralPins.ToAsyncEnumerable();

        private readonly IList<DiscordGuildConfiguration> _guilds;
        private readonly IList<DiscordXIVUser> _users;
        // ReSharper disable CollectionNeverUpdated.Local
        private readonly IList<CachedMessage> _cachedMessages;
        private readonly IList<ChannelDescription> _channelDescriptions;
        private readonly IList<EventReaction> _eventReactions;
        private readonly IList<TimedRole> _timedRoles;
        private readonly IList<Vote> _votes;
        private readonly IList<VoteHost> _voteHosts;
        private readonly IList<EphemeralPin> _ephemeralPins;
        // ReSharper restore CollectionNeverUpdated.Local

        public MemoryDb()
        {
            _guilds = new SynchronizedCollection<DiscordGuildConfiguration>();
            _users = new SynchronizedCollection<DiscordXIVUser>();
            _cachedMessages = new SynchronizedCollection<CachedMessage>();
            _channelDescriptions = new SynchronizedCollection<ChannelDescription>();
            _eventReactions = new SynchronizedCollection<EventReaction>();
            _timedRoles = new SynchronizedCollection<TimedRole>();
            _votes = new SynchronizedCollection<Vote>();
            _voteHosts = new SynchronizedCollection<VoteHost>();
            _ephemeralPins = new SynchronizedCollection<EphemeralPin>();

            Config = new GlobalConfiguration();
        }

        public Task<DiscordXIVUser> GetUserByCharacterInfo(string world, string characterName)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.World == world && u.Name == characterName));
        }

        public Task SetGlobalConfigurationProperty(string key, string value)
        {
            var field = typeof(GlobalConfiguration).GetField(key);
            if (field == null)
                throw new ArgumentException($"Property {key} does not exist on GlobalConfiguration.");
            field.SetValue(Config, value);
            return Task.CompletedTask;
        }

        public async Task SetGuildConfigurationProperty<T>(ulong guildId, string key, T value)
        {
            var existing = _guilds.FirstOrDefault(g => g.Id == guildId);
            if (existing == null)
            {
                existing = new DiscordGuildConfiguration(guildId);
                await AddGuild(existing);
            }

            var field = typeof(DiscordGuildConfiguration).GetField(key);
            if (field == null)
                throw new ArgumentException($"Property {key} does not exist on GlobalConfiguration.");
            field.SetValue(existing, value);
        }

        public Task AddGuild(DiscordGuildConfiguration config)
        {
            var existing = _guilds.FirstOrDefault(g => g.Id == config.Id);
            if (existing != null)
                return Task.CompletedTask;
            _guilds.Add(config);
            return Task.CompletedTask;
        }

        public Task<bool> AddEphemeralPin(ulong guildId, ulong channelId, ulong messageId, ulong pinnerRoleId, ulong pinnerId, DateTime pinTime)
        {
            return Task.FromResult(true);
        }

        public Task<bool> RemoveEphemeralPin(ulong messageId)
        {
            return Task.FromResult(true);
        }

        public Task<bool> AddVoteHost(ulong messageId, ulong ownerId)
        {
            return Task.FromResult(true);
        }

        public Task<bool> RemoveVoteHost(ulong messageId)
        {
            return Task.FromResult(true);
        }

        public Task<bool> AddEventReaction(ulong eventId, ulong userId)
        {
            if (_eventReactions.Any(er => er.EventId == eventId && er.UserId == userId))
                return Task.FromResult(false);
            _eventReactions.Add(new EventReaction { EventId = eventId, UserId = userId });
            return Task.FromResult(true);
        }

        public Task UpdateEventReaction(EventReaction updated)
        {
            var existing = _eventReactions.FirstOrDefault(er => er.EventId == updated.EventId && er.UserId == updated.UserId);
            if (existing != null)
                _eventReactions.Remove(existing);
            _eventReactions.Add(updated);
            return Task.CompletedTask;
        }

        public Task<bool> RemoveEventReaction(ulong eventId, ulong userId)
        {
            var existing = _eventReactions.FirstOrDefault(er => er.EventId == eventId && er.UserId == userId);
            if (existing == null)
                return Task.FromResult(false);
            _eventReactions.Remove(existing);
            return Task.FromResult(true);
        }

        public Task RemoveAllEventReactions(ulong eventId)
        {
            if (_eventReactions.All(er => er.EventId != eventId))
                return Task.FromResult(false);
            _eventReactions.RemoveAll(er => er.EventId == eventId);
            return Task.FromResult(true);
        }

        public Task AddTimedRole(ulong roleId, ulong guildId, ulong userId, DateTime removalTime)
        {
            return Task.CompletedTask;
        }

        public Task RemoveTimedRole(ulong roleId, ulong userId)
        {
            return Task.CompletedTask;
        }

        public Task ConfigureRole(ulong guildId, string roleName, ulong roleId)
        {
            return Task.CompletedTask;
        }

        public Task DeconfigureRole(ulong guildId, string roleName)
        {
            return Task.CompletedTask;
        }

        public Task ConfigureRoleEmote(ulong guildId, ulong roleId, string emoteId)
        {
            return Task.CompletedTask;
        }

        public Task DeconfigureRoleEmote(ulong guildId, string emoteId)
        {
            return Task.CompletedTask;
        }

        public Task AddGuildTextDenylistEntry(ulong guildId, string regexString)
        {
            return Task.CompletedTask;
        }

        public Task RemoveGuildTextDenylistEntry(ulong guildId, string regexString)
        {
            return Task.CompletedTask;
        }

        public Task AddGuildTextGreylistEntry(ulong guildId, string regexString)
        {
            return Task.CompletedTask;
        }

        public Task RemoveGuildTextGreylistEntry(ulong guildId, string regexString)
        {
            return Task.CompletedTask;
        }

        public Task AddUser(DiscordXIVUser user)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateUser(DiscordXIVUser user)
        {
            var i = _users.IndexOf(u => u.DiscordId == user.DiscordId);
            _users[i] = user;
            return Task.CompletedTask;
        }

        public Task<bool> RemoveUser(string world, string name)
        {
            var i = _users.IndexOf(u => string.Equals(u.World, world, StringComparison.InvariantCultureIgnoreCase)
                                        && string.Equals(u.Name, name, StringComparison.InvariantCultureIgnoreCase));
            _users.RemoveAt(i);
            return Task.FromResult(true);
        }

        public Task<bool> RemoveUser(ulong lodestoneId)
        {
            var i = _users.IndexOf(u => u.LodestoneId == lodestoneId.ToString());
            _users.RemoveAt(i);
            return Task.FromResult(true);
        }

        public Task RemoveBrokenUsers()
        {
            return Task.CompletedTask;
        }
        
        public Task CacheMessage(CachedMessage message)
        {
            return Task.CompletedTask;
        }

        public Task DeleteMessage(ulong messageId)
        {
            return Task.CompletedTask;
        }

        public Task UpdateCachedMessage(CachedMessage message)
        {
            return Task.CompletedTask;
        }

        public Task AddChannelDescription(ulong channelId, string description)
        {
            return Task.CompletedTask;
        }

        public Task DeleteChannelDescription(ulong channelId)
        {
            return Task.CompletedTask;
        }

        public Task<bool> AddVote(ulong messageId, ulong userId, string reactionName)
        {
            return Task.FromResult(true);
        }

        public Task<bool> RemoveVote(ulong messageId, ulong userId)
        {
            return Task.FromResult(true);
        }
    }
}