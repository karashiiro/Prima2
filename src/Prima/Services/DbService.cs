using MongoDB.Driver;
using Prima.Game.FFXIV;
using Prima.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Prima.Services
{
    public class DbService : IDbService
    {
        private const string ConnectionString = "mongodb://localhost:27017";
        private const string DbName = "PrimaDb";
        private const double LockTimeoutSeconds = 30;

        // Hide types of the database implementation from callers.
        public GlobalConfiguration Config
        {
            get
            {
                if (!_config.AsQueryable().Any())
                    AddGlobalConfiguration().GetAwaiter().GetResult();
                return _config.AsQueryable().First();
            }
        }

        public IEnumerable<DiscordGuildConfiguration> Guilds => _guildConfig.AsQueryable();
        public IEnumerable<DiscordXIVUser> Users => _users.AsQueryable();
        public IEnumerable<CachedMessage> CachedMessages => _messageCache.AsQueryable();
        public IEnumerable<ChannelDescription> ChannelDescriptions => _channelDescriptions.AsQueryable();
        public IAsyncEnumerable<EventReaction> EventReactions => _eventReactions.AsQueryable().ToAsyncEnumerable();
        public IAsyncEnumerable<TimedRole> TimedRoles => _timedRoles.AsQueryable().ToAsyncEnumerable();
        public IAsyncEnumerable<Vote> Votes => _votes.AsQueryable().ToAsyncEnumerable();
        public IAsyncEnumerable<VoteHost> VoteHosts => _voteHosts.AsQueryable().ToAsyncEnumerable();
        public IAsyncEnumerable<EphemeralPin> EphemeralPins => _ephemeralPins.AsQueryable().ToAsyncEnumerable();

        private readonly IMongoCollection<GlobalConfiguration> _config;
        private readonly IMongoCollection<DiscordGuildConfiguration> _guildConfig;
        private readonly IMongoCollection<DiscordXIVUser> _users;
        private readonly IMongoCollection<CachedMessage> _messageCache;
        private readonly IMongoCollection<ChannelDescription> _channelDescriptions;
        private readonly IMongoCollection<EventReaction> _eventReactions;
        private readonly IMongoCollection<TimedRole> _timedRoles;
        private readonly IMongoCollection<Vote> _votes;
        private readonly IMongoCollection<VoteHost> _voteHosts;
        private readonly IMongoCollection<EphemeralPin> _ephemeralPins;

        private readonly ILogger<DbService> _logger;

        private readonly SemaphoreSlim _lock;

        public DbService(ILogger<DbService> logger)
        {
            _logger = logger;
            _lock = new SemaphoreSlim(80, 80);

            var client = new MongoClient(ConnectionString);
            var database = client.GetDatabase(DbName);

            _config = database.GetCollection<GlobalConfiguration>("GlobalConfiguration");
            _logger.LogInformation("Global configuration status: {DbStatus} documents found",
                _config.EstimatedDocumentCount());

            _guildConfig = database.GetCollection<DiscordGuildConfiguration>("GuildConfiguration");
            _logger.LogInformation("Guild configuration collection status: {DbStatus} documents found",
                _guildConfig.EstimatedDocumentCount());

            _users = database.GetCollection<DiscordXIVUser>("Users");
            _logger.LogInformation("User collection status: {DbStatus} documents found",
                _users.EstimatedDocumentCount());

            _messageCache = database.GetCollection<CachedMessage>("CachedMessages");
            _logger.LogInformation("Message cache collection status: {DbStatus} documents found",
                _messageCache.EstimatedDocumentCount());

            _channelDescriptions = database.GetCollection<ChannelDescription>("ChannelDescriptions");
            _logger.LogInformation("Channel description collection status: {DbStatus} documents found",
                _channelDescriptions.EstimatedDocumentCount());

            _eventReactions = database.GetCollection<EventReaction>("EventReactions");
            _logger.LogInformation("Event reaction collection status: {DbStatus} documents found",
                _eventReactions.EstimatedDocumentCount());

            _timedRoles = database.GetCollection<TimedRole>("TimedRoles");
            _logger.LogInformation("Timed role collection status: {DbStatus} documents found",
                _timedRoles.EstimatedDocumentCount());

            _votes = database.GetCollection<Vote>("Votes");
            _logger.LogInformation("Vote collection status: {DbStatus} documents found",
                _votes.EstimatedDocumentCount());

            _voteHosts = database.GetCollection<VoteHost>("VoteHosts");
            _logger.LogInformation("Vote host collection status: {DbStatus} documents found",
                _voteHosts.EstimatedDocumentCount());

            _ephemeralPins = database.GetCollection<EphemeralPin>("EphemeralPins");
            _logger.LogInformation("Ephemeral pin collection status: {DbStatus} documents found",
                _ephemeralPins.EstimatedDocumentCount());
        }

        public Task<DiscordXIVUser?> GetUserByCharacterInfo(string? world, string? characterName)
        {
            return WithLock(async () =>
            {
                if (characterName == null || world == null) return null;
                _logger.LogInformation("Fetching user: ({World}) {CharacterName}", world, characterName);
                return await _users.Find(u => u.World == world && u.Name == characterName).FirstOrDefaultAsync();
            });
        }

        public Task<DiscordXIVUser?> GetUserByDiscordId(ulong discordId)
        {
            return WithLock(async () =>
            {
                _logger.LogInformation("Fetching user by Discord ID: {DiscordId}", discordId);
                return await _users.Find(u => u.DiscordId == discordId).FirstOrDefaultAsync();
            });
        }

        public async Task SetGlobalConfigurationProperty(string key, string value)
        {
            _logger.LogInformation("Setting global configuration property: {ConfigKey}={ConfigValue}", key, value);
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
            _logger.LogInformation(
                "Setting guild configuration property for guild {GuildId}: {ConfigKey}={ConfigValue}", guildId, key,
                value);
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
                _logger.LogInformation("Creating global configuration");
                await _config.InsertOneAsync(new GlobalConfiguration());
            }
        }

        public async Task AddGuild(DiscordGuildConfiguration config)
        {
            if (!await (await _guildConfig.FindAsync(guild => guild.Id == config.Id)).AnyAsync().ConfigureAwait(false))
            {
                _logger.LogInformation("Creating guild configuration for guild {GuildId}", config.Id);
                await _guildConfig.InsertOneAsync(config);
            }
        }

        public async Task<bool> AddEphemeralPin(ulong guildId, ulong channelId, ulong messageId, ulong pinnerRoleId,
            ulong pinnerId, DateTime pinTime)
        {
            _logger.LogInformation(
                "Creating ephemeral pin in channel {ChannelId} in guild {GuildId}: messageId={MessageId}",
                channelId, guildId, messageId);
            var existingSet = await _ephemeralPins.FindAsync(e => e.MessageId == messageId);
            if (await existingSet.AnyAsync())
            {
                var update = Builders<EphemeralPin>.Update.Set("PinTime", pinTime);
                await _ephemeralPins.UpdateOneAsync(e => e.MessageId == messageId, update);
                return true;
            }

            var newEphemeralPin = new EphemeralPin
            {
                GuildId = guildId, ChannelId = channelId, MessageId = messageId, PinnerRoleId = pinnerRoleId,
                PinnerId = pinnerId, PinTime = pinTime
            };
            await _ephemeralPins.InsertOneAsync(newEphemeralPin);
            return true;
        }

        public async Task<bool> RemoveEphemeralPin(ulong messageId)
        {
            _logger.LogInformation("Removing ephemeral pin: messageId={MessageId}", messageId);
            var existingSet = await _ephemeralPins.FindAsync(e => e.MessageId == messageId);
            if (!await existingSet.AnyAsync()) return false;
            await _ephemeralPins.DeleteManyAsync(e => e.MessageId == messageId);
            return true;
        }

        public async Task<bool> AddVoteHost(ulong messageId, ulong ownerId)
        {
            _logger.LogInformation("Adding vote host: messageId={MessageId}, ownerId={UserId}", messageId, ownerId);
            var existingSet = await _voteHosts.FindAsync(v => v.MessageId == messageId);
            if (await existingSet.AnyAsync()) return false;
            var voteHost = new VoteHost { MessageId = messageId, OwnerId = ownerId };
            await _voteHosts.InsertOneAsync(voteHost);
            return true;
        }

        public async Task<bool> RemoveVoteHost(ulong messageId)
        {
            _logger.LogInformation("Removing vote host: messageId={MessageId}", messageId);
            var existingSet = await _voteHosts.FindAsync(v => v.MessageId == messageId);
            if (!await existingSet.AnyAsync()) return false;
            await _voteHosts.DeleteManyAsync(v => v.MessageId == messageId);
            return true;
        }

        private async Task AddGuildIfAbsent(ulong guildId)
        {
            if (!await (await _guildConfig.FindAsync(guild => guild.Id == guildId)).AnyAsync().ConfigureAwait(false))
            {
                _logger.LogInformation("Creating guild configuration for guild {GuildId}", guildId);
                await _guildConfig.InsertOneAsync(new DiscordGuildConfiguration(guildId));
            }
        }

        public async Task<bool> AddVote(ulong messageId, ulong userId, string reactionName)
        {
            _logger.LogInformation("Adding vote: messageId={MessageId}, userId={UserId}, reaction={ReactionName}",
                messageId, userId, reactionName);
            var existingSet = await _votes.FindAsync(v => v.MessageId == messageId && v.ReactionUserId == userId);
            if (await existingSet.AnyAsync()) return false;
            var vote = new Vote { MessageId = messageId, ReactionUserId = userId, ReactionName = reactionName };
            await _votes.InsertOneAsync(vote);
            return true;
        }

        public async Task<bool> RemoveVote(ulong messageId, ulong userId)
        {
            _logger.LogInformation("Removing vote: messageId={MessageId}, userId={UserId}", messageId, userId);
            var existingSet = await _votes.FindAsync(v => v.MessageId == messageId && v.ReactionUserId == userId);
            if (!await existingSet.AnyAsync()) return false;
            await _votes.DeleteOneAsync(v => v.MessageId == messageId && v.ReactionUserId == userId);
            return true;
        }

        public async Task<bool> AddEventReaction(ulong eventId, ulong userId)
        {
            _logger.LogInformation("Adding event reaction: eventId={EventId}, userId={UserId}", eventId, userId);
            var existingSet = await _eventReactions.FindAsync(er => er.EventId == eventId && er.UserId == userId);
            if (await existingSet.AnyAsync()) return false;
            var newEventReaction = new EventReaction { EventId = eventId, UserId = userId };
            await _eventReactions.InsertOneAsync(newEventReaction);
            return true;
        }

        public async Task UpdateEventReaction(EventReaction updated)
        {
            _logger.LogInformation("Updating event reaction: eventId={EventId}, userId={UserId}", updated.EventId,
                updated.UserId);
            await RemoveEventReaction(updated.EventId, updated.UserId);
            await AddEventReaction(updated.EventId, updated.UserId);
        }

        public async Task<bool> RemoveEventReaction(ulong eventId, ulong userId)
        {
            _logger.LogInformation("Removing event reaction: eventId={EventId}, userId={UserId}", eventId, userId);
            var existingSet = await _eventReactions.FindAsync(er => er.EventId == eventId && er.UserId == userId);
            if (!await existingSet.AnyAsync()) return false;
            await _eventReactions.DeleteOneAsync(er => er.EventId == eventId && er.UserId == userId);
            return true;
        }

        public async Task RemoveAllEventReactions(ulong eventId)
        {
            _logger.LogInformation("Removing all event reactions for event {EventId}", eventId);
            var existingSet = await _eventReactions.FindAsync(er => er.EventId == eventId);
            if (!await existingSet.AnyAsync()) return;
            await _eventReactions.DeleteManyAsync(er => er.EventId == eventId);
        }

        public async Task AddTimedRole(ulong roleId, ulong guildId, ulong userId, DateTime removalTime)
        {
            _logger.LogInformation("Adding timed role {RoleId} to user {UserId} in guild {GuildId}", roleId, userId,
                guildId);

            var existingSet =
                await _timedRoles.FindAsync(tr => tr.RoleId == roleId && tr.GuildId == guildId && tr.UserId == userId);
            if (await existingSet.AnyAsync())
            {
                var update = Builders<TimedRole>.Update.Set("RemovalTime", removalTime);
                await _timedRoles.UpdateOneAsync(
                    tr => tr.RoleId == roleId && tr.GuildId == guildId && tr.UserId == userId, update);
                return;
            }

            var newTimedRole = new TimedRole
                { RoleId = roleId, GuildId = guildId, UserId = userId, RemovalTime = removalTime };
            await _timedRoles.InsertOneAsync(newTimedRole);
        }

        public async Task RemoveTimedRole(ulong roleId, ulong userId)
        {
            _logger.LogInformation("Removing timed role {RoleId} from user {UserId}", roleId, userId);
            var existingSet = await _timedRoles.FindAsync(tr => tr.RoleId == roleId && tr.UserId == userId);
            if (!await existingSet.AnyAsync()) return;
            await _timedRoles.DeleteOneAsync(tr => tr.RoleId == roleId && tr.UserId == userId);
        }

        public Task ConfigureRole(ulong guildId, string roleName, ulong roleId)
        {
            _logger.LogInformation("Configuring role {RoleName} in guild {GuildId}", roleName, guildId);
            var update = Builders<DiscordGuildConfiguration>.Update.Set($"Roles.{roleName}", roleId.ToString());
            return _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
        }

        public Task DeconfigureRole(ulong guildId, string roleName)
        {
            _logger.LogInformation("Deconfiguring role {RoleName} in guild {GuildId}", roleName, guildId);
            var update = Builders<DiscordGuildConfiguration>.Update.Unset($"Roles.{roleName}");
            return _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
        }

        public Task ConfigureRoleEmote(ulong guildId, ulong roleId, string emoteId)
        {
            _logger.LogInformation("Configuring role emote {EmoteId} for role {RoleId} in guild {GuildId}", emoteId,
                roleId, guildId);
            var update = Builders<DiscordGuildConfiguration>.Update.Set($"RoleEmotes.{emoteId}", roleId.ToString());
            return _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
        }

        public Task DeconfigureRoleEmote(ulong guildId, string emoteId)
        {
            _logger.LogInformation("Deconfiguring role emote {EmoteId} in guild {GuildId}", emoteId, guildId);
            var update = Builders<DiscordGuildConfiguration>.Update.Unset($"RoleEmotes.{emoteId}");
            return _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
        }

        public Task AddGuildTextDenylistEntry(ulong guildId, string regexString)
        {
            var update = Builders<DiscordGuildConfiguration>.Update.Push("TextBlacklist", regexString);
            return _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
        }

        public async Task RemoveGuildTextDenylistEntry(ulong guildId, string regexString)
        {
            var denylist = (await (await _guildConfig.FindAsync(guild => guild.Id == guildId)).FirstAsync()
                .ConfigureAwait(false)).TextDenylist;
            if (denylist.Any())
            {
                var update = Builders<DiscordGuildConfiguration>.Update.Pull("TextBlacklist", regexString);
                await _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
            }
        }

        public Task AddGuildTextGreylistEntry(ulong guildId, string regexString)
        {
            var update = Builders<DiscordGuildConfiguration>.Update.Push("TextGreylist", regexString);
            return _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
        }

        public async Task RemoveGuildTextGreylistEntry(ulong guildId, string regexString)
        {
            var greylist = (await (await _guildConfig.FindAsync(guild => guild.Id == guildId)).FirstAsync()
                .ConfigureAwait(false)).TextGreylist;
            if (greylist.Any())
            {
                var update = Builders<DiscordGuildConfiguration>.Update.Pull("TextGreylist", regexString);
                await _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
            }
        }

        public Task AddUser(DiscordXIVUser user)
        {
            return WithLock(async () =>
            {
                _logger.LogInformation("Registering user {UserId}", user.DiscordId);
                if (await (await _users.FindAsync(u => u.DiscordId == user.DiscordId)).AnyAsync().ConfigureAwait(false))
                    await _users.DeleteOneAsync(u => u.DiscordId == user.DiscordId);
                await _users.InsertOneAsync(user);
            });
        }

        public Task UpdateUser(DiscordXIVUser user) => AddUser(user);

        public Task<bool> RemoveUser(string world, string name)
        {
            return WithLock(async () =>
            {
                _logger.LogInformation("Unregistering user with character ({World}) {CharacterName}", world, name);
                var filterBuilder = Builders<DiscordXIVUser>.Filter;
                var filter = filterBuilder.Eq(props => props.World, world) & filterBuilder.Eq(props => props.Name, name);
                var result = await _users.DeleteOneAsync(filter);
                return result.DeletedCount > 0;
            });
        }

        public async Task<bool> RemoveUser(ulong lodestoneId)
        {
            _logger.LogInformation("Unregistering user with Lodestone ID {LodestoneId}", lodestoneId);
            var filterBuilder = Builders<DiscordXIVUser>.Filter;
            var filter = filterBuilder.Eq(props => props.LodestoneId, lodestoneId.ToString());
            var result = await _users.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        public Task RemoveBrokenUsers()
        {
            var deleteFilter = Builders<DiscordXIVUser>.Filter.Eq("DiscordId", 0UL);
            return _users.DeleteManyAsync(deleteFilter);
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
            if (await (await _channelDescriptions.FindAsync(cd => cd.ChannelId == channelId)).AnyAsync()
                .ConfigureAwait(false))
                await _channelDescriptions.DeleteOneAsync(cd => cd.ChannelId == channelId);
            await _channelDescriptions.InsertOneAsync(new ChannelDescription
                { ChannelId = channelId, Description = description });
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

        private async Task WithLock(Func<Task> action)
        {
            if (!await _lock.WaitAsync(TimeSpan.FromSeconds(LockTimeoutSeconds)).ConfigureAwait(false))
            {
                _logger.LogWarning("Could not acquire database lock");
                throw new TimeoutException("Could not acquire database lock");
            }

            try
            {
                await action();
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<T> WithLock<T>(Func<Task<T>> action)
        {
            if (!await _lock.WaitAsync(TimeSpan.FromSeconds(LockTimeoutSeconds)).ConfigureAwait(false))
            {
                _logger.LogWarning("Could not acquire database lock");
                throw new TimeoutException("Could not acquire database lock");
            }

            try
            {
                return await action();
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}