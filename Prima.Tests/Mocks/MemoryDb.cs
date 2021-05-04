using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prima.Extensions;
using Prima.Models;
using Prima.Services;

namespace Prima.Tests.Mocks
{
    public class MemoryDb : IDbService
    {
        public GlobalConfiguration Config => _globalConfiguration;

        public IEnumerable<DiscordGuildConfiguration> Guilds => _guilds;
        public IEnumerable<DiscordXIVUser> Users => _users;
        public IEnumerable<ScheduledEvent> Events => _events;
        public IEnumerable<CachedMessage> CachedMessages => _cachedMessages;
        public IEnumerable<ChannelDescription> ChannelDescriptions => _channelDescriptions;
        public IAsyncEnumerable<EventReaction> EventReactions => _eventReactions.ToAsyncEnumerable();
        public IAsyncEnumerable<TimedRole> TimedRoles => _timedRoles.ToAsyncEnumerable();
        public IAsyncEnumerable<Vote> Votes => throw new NotImplementedException();
        public IAsyncEnumerable<VoteHost> VoteHosts => throw new NotImplementedException();
        public IAsyncEnumerable<EphemeralPin> EphemeralPins => throw new NotImplementedException();

        private readonly IList<DiscordGuildConfiguration> _guilds;
        private readonly IList<DiscordXIVUser> _users;
        private readonly IList<ScheduledEvent> _events;
        private readonly IList<CachedMessage> _cachedMessages;
        private readonly IList<ChannelDescription> _channelDescriptions;
        private readonly IList<EventReaction> _eventReactions;
        private readonly IList<TimedRole> _timedRoles;

        private readonly GlobalConfiguration _globalConfiguration;

        public MemoryDb()
        {
            _guilds = new SynchronizedCollection<DiscordGuildConfiguration>();
            _users = new SynchronizedCollection<DiscordXIVUser>();
            _events = new SynchronizedCollection<ScheduledEvent>();
            _cachedMessages = new SynchronizedCollection<CachedMessage>();
            _channelDescriptions = new SynchronizedCollection<ChannelDescription>();
            _eventReactions = new SynchronizedCollection<EventReaction>();
            _timedRoles = new SynchronizedCollection<TimedRole>();

            _globalConfiguration = new GlobalConfiguration();
        }

        public Task SetGlobalConfigurationProperty(string key, string value)
        {
            var field = typeof(GlobalConfiguration).GetField(key);
            if (field == null)
                throw new ArgumentException($"Property {key} does not exist on GlobalConfiguration.");
            field.SetValue(_globalConfiguration, value);
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
            throw new NotImplementedException();
        }

        public Task<bool> RemoveEphemeralPin(ulong messageId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AddVoteHost(ulong messageId, ulong ownerId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveVoteHost(ulong messageId)
        {
            throw new NotImplementedException();
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

        public Task AddGuildTextBlacklistEntry(ulong guildId, string regexString)
        {
            return Task.CompletedTask;
        }

        public Task RemoveGuildTextBlacklistEntry(ulong guildId, string regexString)
        {
            return Task.CompletedTask;
        }

        public Task AddUser(DiscordXIVUser user)
        {
            return Task.CompletedTask;
        }

        public Task UpdateUser(DiscordXIVUser user)
        {
            return Task.CompletedTask;
        }

        public Task RemoveBrokenUsers()
        {
            return Task.CompletedTask;
        }

        public Task AddScheduledEvent(ScheduledEvent @event)
        {
            return Task.CompletedTask;
        }

        public Task UpdateScheduledEvent(ScheduledEvent newEvent)
        {
            return Task.CompletedTask;
        }

        public Task AddMemberToEvent(ScheduledEvent @event, ulong memberId)
        {
            return Task.CompletedTask;
        }

        public Task RemoveMemberToEvent(ScheduledEvent @event, ulong memberId)
        {
            return Task.CompletedTask;
        }

        public Task<ScheduledEvent> TryRemoveScheduledEvent(DateTime when, ulong userId)
        {
            return Task.FromResult<ScheduledEvent>(null);
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
            throw new NotImplementedException();
        }

        public Task<bool> RemoveVote(ulong messageId, ulong userId)
        {
            throw new NotImplementedException();
        }
    }
}