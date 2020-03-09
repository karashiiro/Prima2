using MongoDB.Driver;
using Prima.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prima.Services
{
    public class DbService
    {
        // Hide types of the database implementation from callers.
        public GlobalConfiguration Config { get => _config.AsQueryable().ToEnumerable().First(); }
        public IEnumerable<DiscordGuildConfiguration> Guilds { get => _guildConfig.AsQueryable().ToEnumerable(); }
        public IEnumerable<DiscordXIVUser> Users { get => _users.AsQueryable().ToEnumerable(); }

        private readonly MongoClient _client;
        private readonly IMongoDatabase _database;
        
        private readonly IMongoCollection<GlobalConfiguration> _config;
        private readonly IMongoCollection<DiscordGuildConfiguration> _guildConfig;
        private readonly IMongoCollection<DiscordXIVUser> _users;

        private const string _connectionString = "mongodb://localhost:27017";
        private const string _dbName = "PrimaDb";

        public DbService()
        {
            _client = new MongoClient(_connectionString);
            _database = _client.GetDatabase(_dbName);

            _config = _database.GetCollection<GlobalConfiguration>("GlobalConfiguration");
            Log.Information("Global configuration status: {DbStatus} documents found.", _config.EstimatedDocumentCount());

            _guildConfig = _database.GetCollection<DiscordGuildConfiguration>("GuildConfiguration");
            Log.Information("Guild configuration status: {DbStatus} documents found.", _guildConfig.EstimatedDocumentCount());

            _users = _database.GetCollection<DiscordXIVUser>("Users");
            Log.Information("User database status: {DbStatus} documents found.", _users.EstimatedDocumentCount());
        }

        public async Task SetGlobalConfigurationProperty(string key, string value)
        {
            if (!Config.HasFieldOrProperty(key))
            {
                throw new ArgumentException($"Property {key} does not exist on GlobalConfiguration.");
            }
            var update = Builders<GlobalConfiguration>.Update.Set(key, value);
            await _config.UpdateOneAsync(config => true, update);
        }

        public async Task SetGuildConfigurationProperty(ulong guildId, string key, string value)
        {
            if (!new DiscordGuildConfiguration(0).HasFieldOrProperty(key))
            {
                throw new ArgumentException($"Property {key} does not exist on DiscordGuildConfiguration.");
            }
            var update = Builders<DiscordGuildConfiguration>.Update.Set(key, value);
            await _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
        }

        public async Task AddGlobalConfiguration()
        {
            if (await _config.CountDocumentsAsync(FilterDefinition<GlobalConfiguration>.Empty) == 0)
            {
                await _config.InsertOneAsync(new GlobalConfiguration());
            }
        }

        public async Task AddGuild(DiscordGuildConfiguration config)
        {
            if (!(await _guildConfig.FindAsync(guild => guild.Id == config.Id)).Any())
            {
                await _guildConfig.InsertOneAsync(config);
            }
        }

        public async Task ConfigureRole(ulong guildId, string roleName, ulong roleId)
        {
            var update = Builders<DiscordGuildConfiguration>.Update.Set($"Roles.{roleName}", roleId.ToString());
            await _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
        }

        public async Task AddGuildTextBlacklistEntry(ulong guildId, string regexString)
        {
            var update = Builders<DiscordGuildConfiguration>.Update.Push("TextBlacklist", regexString);
            await _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
        }

        public async Task RemoveGuildTextBlacklistEntry(ulong guildId, string regexString)
        {
            var blacklist = (await _guildConfig.FindAsync(guild => guild.Id == guildId)).First().TextBlacklist;
            if (blacklist.Any())
            {
                var update = Builders<DiscordGuildConfiguration>.Update.Pull("TextBlacklist", regexString);
                await _guildConfig.UpdateOneAsync(guild => guild.Id == guildId, update);
            }
        }

        public async Task AddUser(DiscordXIVUser user)
        {
            if ((await _users.FindAsync(u => u.DiscordId == user.DiscordId)).Any())
                await _users.DeleteOneAsync(u => u.DiscordId == user.DiscordId);
            await _users.InsertOneAsync(user);
        }
    }
}
