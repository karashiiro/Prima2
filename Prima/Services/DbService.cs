using MongoDB.Driver;
using Prima.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prima.Services
{
    public class DbService : IDbService
    {
        private const string ConnectionString = "mongodb://localhost:27017";
        private const string DbName = "PrimaDb";

        // Hide types of the database implementation from callers.
        public GlobalConfiguration Config
        {
            get
            {
                if (!_config.AsQueryable().Any())
                    AddGlobalConfiguration().GetAwaiter().GetResult();
                return _config.AsQueryable().ToEnumerable().First();
            }
        }

        public IEnumerable<DiscordGuildConfiguration> Guilds => _guildConfig.AsQueryable().ToEnumerable();
        public IEnumerable<DiscordXIVUser> Users => _users.AsQueryable().ToEnumerable();
        public IEnumerable<ScheduledEvent> Events => _events.AsQueryable().ToEnumerable();
        public IEnumerable<CachedMessage> CachedMessages => _messageCache.AsQueryable().ToEnumerable();
        public IEnumerable<ChannelDescription> ChannelDescriptions => _channelDescriptions.AsQueryable().ToEnumerable();
        public IAsyncEnumerable<EventReaction> EventReactions => _eventReactions.AsQueryable().ToAsyncEnumerable();
        public IAsyncEnumerable<TimedRole> TimedRoles => _timedRoles.AsQueryable().ToAsyncEnumerable();

        private readonly IMongoCollection<GlobalConfiguration> _config;
        private readonly IMongoCollection<DiscordGuildConfiguration> _guildConfig;
        private readonly IMongoCollection<DiscordXIVUser> _users;
        private readonly IMongoCollection<ScheduledEvent> _events;
        private readonly IMongoCollection<CachedMessage> _messageCache;
        private readonly IMongoCollection<ChannelDescription> _channelDescriptions;
        private readonly IMongoCollection<EventReaction> _eventReactions;
        private readonly IMongoCollection<TimedRole> _timedRoles;

        public DbService()
        {
            var client = new MongoClient(ConnectionString);
            var database = client.GetDatabase(DbName);

            _config = database.GetCollection<GlobalConfiguration>("GlobalConfiguration");
            Log.Information("Global configuration status: {DbStatus} documents found.", _config.EstimatedDocumentCount());

            _guildConfig = database.GetCollection<DiscordGuildConfiguration>("GuildConfiguration");
            Log.Information("Guild configuration collection status: {DbStatus} documents found.", _guildConfig.EstimatedDocumentCount());

            _users = database.GetCollection<DiscordXIVUser>("Users");
            Log.Information("User collection status: {DbStatus} documents found.", _users.EstimatedDocumentCount());

            _events = database.GetCollection<ScheduledEvent>("ScheduledEvents");
            Log.Information("Event collection status: {DbStatus} documents found.", _events.EstimatedDocumentCount());

            _messageCache = database.GetCollection<CachedMessage>("CachedMessages");
            Log.Information("Message cache collection status: {DbStatus} documents found.", _messageCache.EstimatedDocumentCount());

            _channelDescriptions = database.GetCollection<ChannelDescription>("ChannelDescriptions");
            Log.Information("Channel description collection status: {DbStatus} documents found.", _channelDescriptions.EstimatedDocumentCount());

            _eventReactions = database.GetCollection<EventReaction>("EventReactions");
            Log.Information("Event reaction collection status: {DbStatus} documents found.", _eventReactions.EstimatedDocumentCount());

            _timedRoles = database.GetCollection<TimedRole>("TimedRoles");
            Log.Information("Timed role collection status: {DbStatus} documents found.", _timedRoles.EstimatedDocumentCount());
        }

        public async Task SetGlobalConfigurationProperty(string key, string value)
        {
            await AddGlobalConfiguration();
            if (!Config.HasFieldOrProperty(key))
            {
                throw new ArgumentException($"Property {key} does not exist on GlobalConfiguration.");
            }
            var update = Builders<GlobalConfiguration>.Update.Set(key, value);
            await _config.UpdateOneAsync(config => true, update);
        }

        public async Task SetGuildConfigurationProperty<T>(ulong guildId, string key, T value)
        {
            await AddGuildIfAbsent(guildId);
            if (!new DiscordGuildConfiguration(0).HasFieldOrProperty(key))
            {
                throw new ArgumentException($"Property {key} does not exist on DiscordGuildConfiguration.");
            }
            var update = Builders<DiscordGuildConfiguration>.Update.Set(key, value);
            await _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
        }

        private async Task AddGlobalConfiguration()
        {
            if (await _config.CountDocumentsAsync(FilterDefinition<GlobalConfiguration>.Empty) == 0)
            {
                await _config.InsertOneAsync(new GlobalConfiguration());
            }
        }

        public async Task AddGuild(DiscordGuildConfiguration config)
        {
            if (!await (await _guildConfig.FindAsync(guild => guild.Id == config.Id)).AnyAsync().ConfigureAwait(false))
            {
                await _guildConfig.InsertOneAsync(config);
            }
        }

        private async Task AddGuildIfAbsent(ulong guildId)
        {
            if (!await (await _guildConfig.FindAsync(guild => guild.Id == guildId)).AnyAsync().ConfigureAwait(false))
            {
                await _guildConfig.InsertOneAsync(new DiscordGuildConfiguration(guildId));
            }
        }

        public async Task<bool> AddEventReaction(ulong eventId, ulong userId)
        {
            var existingSet = await _eventReactions.FindAsync(er => er.EventId == eventId && er.UserId == userId);
            if (await existingSet.AnyAsync()) return false;
            var newEventReaction = new EventReaction { EventId = eventId, UserId = userId };
            await _eventReactions.InsertOneAsync(newEventReaction);
            return true;
        }

        public async Task UpdateEventReaction(EventReaction updated)
        {
            await RemoveEventReaction(updated.EventId, updated.UserId);
            await AddEventReaction(updated.EventId, updated.UserId);
        }

        public async Task<bool> RemoveEventReaction(ulong eventId, ulong userId)
        {
            var existingSet = await _eventReactions.FindAsync(er => er.EventId == eventId && er.UserId == userId);
            if (!await existingSet.AnyAsync()) return false;
            await _eventReactions.DeleteManyAsync(er => er.EventId == eventId && er.UserId == userId);
            return true;
        }

        public async Task RemoveAllEventReactions(ulong eventId)
        {
            var existingSet = await _eventReactions.FindAsync(er => er.EventId == eventId);
            if (!await existingSet.AnyAsync()) return;
            await _eventReactions.DeleteManyAsync(er => er.EventId == eventId);
        }

        public async Task AddTimedRole(ulong roleId, ulong guildId, ulong userId, DateTime removalTime)
        {
            var existingSet = await _timedRoles.FindAsync(tr => tr.RoleId == roleId && tr.GuildId == guildId && tr.UserId == userId);
            if (await existingSet.AnyAsync())
            {
                var update = Builders<TimedRole>.Update.Set("RemovalTime", removalTime);
                await _timedRoles.UpdateOneAsync(tr => tr.RoleId == roleId && tr.GuildId == guildId && tr.UserId == userId, update);
                return;
            }

            var newTimedRole = new TimedRole { RoleId = roleId, GuildId = guildId, UserId = userId, RemovalTime = removalTime };
            await _timedRoles.InsertOneAsync(newTimedRole);
        }

        public async Task RemoveTimedRole(ulong roleId, ulong userId)
        {
            var existingSet = await _timedRoles.FindAsync(tr => tr.RoleId == roleId && tr.UserId == userId);
            if (!await existingSet.AnyAsync()) return;
            await _timedRoles.DeleteManyAsync(tr => tr.RoleId == roleId && tr.UserId == userId);
        }

        public Task ConfigureRole(ulong guildId, string roleName, ulong roleId)
        {
            var update = Builders<DiscordGuildConfiguration>.Update.Set($"Roles.{roleName}", roleId.ToString());
            return _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
        }

        public Task DeconfigureRole(ulong guildId, string roleName)
        {
            var update = Builders<DiscordGuildConfiguration>.Update.Unset($"Roles.{roleName}");
            return _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
        }

        public Task ConfigureRoleEmote(ulong guildId, ulong roleId, string emoteId)
        {
            var update = Builders<DiscordGuildConfiguration>.Update.Set($"RoleEmotes.{emoteId}", roleId.ToString());
            return _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
        }

        public Task DeconfigureRoleEmote(ulong guildId, string emoteId)
        {
            var update = Builders<DiscordGuildConfiguration>.Update.Unset($"RoleEmotes.{emoteId}");
            return _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
        }

        public Task AddGuildTextBlacklistEntry(ulong guildId, string regexString)
        {
            var update = Builders<DiscordGuildConfiguration>.Update.Push("TextBlacklist", regexString);
            return _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
        }

        public async Task RemoveGuildTextBlacklistEntry(ulong guildId, string regexString)
        {
            var blacklist = (await (await _guildConfig.FindAsync(guild => guild.Id == guildId)).FirstAsync().ConfigureAwait(false)).TextBlacklist;
            if (blacklist.Any())
            {
                var update = Builders<DiscordGuildConfiguration>.Update.Pull("TextBlacklist", regexString);
                await _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
            }
        }

        public async Task AddUser(DiscordXIVUser user)
        {
            if (await (await _users.FindAsync(u => u.DiscordId == user.DiscordId)).AnyAsync().ConfigureAwait(false))
                await _users.DeleteOneAsync(u => u.DiscordId == user.DiscordId);
            await _users.InsertOneAsync(user);
        }

        public Task UpdateUser(DiscordXIVUser user) => AddUser(user);

        public Task AddScheduledEvent(ScheduledEvent @event)
            => _events.InsertOneAsync(@event);

        public async Task UpdateScheduledEvent(ScheduledEvent newEvent)
        {
            var existing = await (await _events.FindAsync(e => e.MessageId3 == newEvent.MessageId3)).FirstOrDefaultAsync();
            if (existing == null)
            {
                await AddScheduledEvent(newEvent);
                return;
            }
            await _events.ReplaceOneAsync(e => e.MessageId3 == newEvent.MessageId3, newEvent);
        }

        public async Task AddMemberToEvent(ScheduledEvent @event, ulong memberId)
        {
            var existing = await (await _events.FindAsync(e => e.MessageId3 == @event.MessageId3)).FirstOrDefaultAsync();
            if (existing == null)
            {
                return;
            }
            var update = Builders<ScheduledEvent>.Update.Push("SubscribedUsers", memberId.ToString());
            await _events.UpdateOneAsync(e => e.MessageId3 == @event.MessageId3, update);
        }

        public async Task RemoveMemberToEvent(ScheduledEvent @event, ulong memberId)
        {
            var existing = await (await _events.FindAsync(e => e.MessageId3 == @event.MessageId3)).FirstOrDefaultAsync();
            if (existing == null)
            {
                return;
            }
            var update = Builders<ScheduledEvent>.Update.Pull("SubscribedUsers", memberId.ToString());
            await _events.UpdateOneAsync(e => e.MessageId3 == @event.MessageId3, update);
        }

        public async Task<ScheduledEvent> TryRemoveScheduledEvent(DateTime when, ulong userId)
        {
            var existing = await _events.FindAsync(e => e.RunTime == when.ToBinary() && e.LeaderId == userId);
            var @event = await existing.FirstOrDefaultAsync();
            if (@event != null)
            {
                await _events.DeleteOneAsync(e => e.RunTime == when.ToBinary());
            }
            return @event;
        }

        public Task CacheMessage(CachedMessage message)
            => _messageCache.InsertOneAsync(message);

        public async Task DeleteMessage(ulong messageId)
        {
            var existing = await _messageCache.FindAsync(cm => cm.MessageId == messageId);
            var message = await existing.FirstOrDefaultAsync();
            if (message != null)
            {
                await _messageCache.DeleteOneAsync(cm => cm.MessageId == messageId);
            }
        }

        public async Task UpdateCachedMessage(CachedMessage message)
        {
            await DeleteMessage(message.MessageId);
            await CacheMessage(message);
        }

        public async Task AddChannelDescription(ulong channelId, string description)
        {
            if (await (await _channelDescriptions.FindAsync(cd => cd.ChannelId == channelId)).AnyAsync().ConfigureAwait(false))
                await _channelDescriptions.DeleteOneAsync(cd => cd.ChannelId == channelId);
            await _channelDescriptions.InsertOneAsync(new ChannelDescription { ChannelId = channelId, Description = description });
        }

        public async Task DeleteChannelDescription(ulong channelId)
        {
            var existing = await _channelDescriptions.FindAsync(cd => cd.ChannelId == channelId);
            var message = await existing.FirstOrDefaultAsync();
            if (message != null)
            {
                await _channelDescriptions.DeleteOneAsync(cd => cd.ChannelId == channelId);
            }
        }
    }
}
