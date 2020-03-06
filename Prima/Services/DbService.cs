﻿using MongoDB.Driver;
using Prima.Models;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prima.Services
{
    public class DbService
    {
        // Hide types of the database implementation from callers.
        public GlobalConfiguration Config { get; private set; }

        public IEnumerable<DiscordGuildConfiguration> Guilds { get; private set; }
        public IEnumerable<DiscordXIVUser> Users { get; private set; }

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
            Config = _config.AsQueryable().ToEnumerable().First();
            Log.Information("Global configuration status: {DbStatus} documents found.", _config.EstimatedDocumentCount());

            _guildConfig = _database.GetCollection<DiscordGuildConfiguration>("GuildConfiguration");
            Guilds = _guildConfig.AsQueryable().ToEnumerable();
            Log.Information("Guild configuration status: {DbStatus} documents found.", _guildConfig.EstimatedDocumentCount());

            _users = _database.GetCollection<DiscordXIVUser>("Users");
            Users = _users.AsQueryable().ToEnumerable();
            Log.Information("User database status: {DbStatus} documents found.", _users.EstimatedDocumentCount());
        }

        public async Task AddGuild(DiscordGuildConfiguration config)
        {
            if ((await _guildConfig.FindAsync(guild => guild.Id == config.Id)).Any())
            {
                var filter = Builders<DiscordGuildConfiguration>.Filter.Eq("Id", config.Id);
                var update = Builders<DiscordGuildConfiguration>.Update.Set("Id", config.Id);
                await _guildConfig.UpdateOneAsync(guild => guild.Id == config.Id, update);
            }
            else
            {
                await _guildConfig.InsertOneAsync(config);
            }
        }

        public async Task AddGuildTextBlacklistEntry(ulong guildId, string regexString)
        {
            using var watch = await _guildConfig.WatchAsync();
            var filter = Builders<DiscordGuildConfiguration>.Filter.Eq("Id", guildId);
            _guildConfig
                .FindAsync(filter)
                .Result
                .First()
                .TextBlacklist
                .Add(regexString);
            return;
        }

        public async Task RemoveGuildTextBlacklistEntry(ulong guildId, string regexString)
        {
            using var watch = await _guildConfig.WatchAsync();
            var filter = Builders<DiscordGuildConfiguration>.Filter.Eq("Id", guildId);
            var blacklist = _guildConfig.FindAsync(filter).Result.First().TextBlacklist;
            if (blacklist.Any())
            {
                blacklist.Remove(regexString);
            }
            return;
        }

        public async Task AddUser(DiscordXIVUser user)
        {
            if ((await _users.FindAsync(u => u.DiscordId == user.DiscordId)).Any())
            {
                var filter = Builders<DiscordXIVUser>.Filter.Eq("Id", user.DiscordId);
                var update = Builders<DiscordXIVUser>.Update.Set("Id", user.DiscordId);
                await _users.UpdateOneAsync(u => u.DiscordId == user.DiscordId, update);
            }
            else
            {
                await _users.InsertOneAsync(user);
            }
        }
    }
}
