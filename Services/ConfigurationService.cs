using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Prima.Contexts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prima.Services
{
    public class ConfigurationService
    {
        /// <summary>
        /// Gets the current <see cref="Preset"/> of the bot.
        /// </summary>
        public Preset CurrentPreset { get; private set; }

        private IConfigurationRoot _config;
        public ConfigurationService(Preset preset)
        {
            CurrentPreset = preset;
            BuildConfiguration();
        }

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        public IConfigurationSection GetSection(string key) => _config.GetSection(key);

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        public IConfigurationSection GetSection(params string[] keys)
        {
            IConfigurationSection cursor = _config.GetSection(keys[0]);
            for (int i = 1; i < keys.Length; i++)
            {
                cursor = cursor.GetSection(keys[i]);
            }
            return cursor;
        }

        /// <summary>
        /// Gets a configuration sub-section with the specified key as a <see cref="ulong"/>.
        /// </summary>
        public ulong GetULong(string key)
        {
            IConfigurationSection section = GetSection(key);
            return ulong.Parse(section.Value);
        }

        /// <summary>
        /// Gets a configuration sub-section with the specified key as a <see cref="ulong"/>.
        /// </summary>
        public ulong GetULong(params string[] keys)
        {
            IConfigurationSection section = GetSection(keys);
            return ulong.Parse(section.Value);
        }

        /// <summary>
        /// Get the stored clock configurations.
        /// </summary>
        public static async Task<IList<ClockConfiguration>> GetClockData()
        {
            IList<ClockConfiguration> configurations;
            using var db = new ConfigurationContext();
            configurations = await db.ClockData.ToListAsync();
            return configurations;
        }

        /// <summary>
        /// Save a clock configuration.
        /// </summary>
        public static async Task SaveClock(ulong gid, ulong cid, string tzid)
        {
            using var db = new ConfigurationContext();
            if (db.ClockData.SingleAsync(clock => clock.GuildId == gid & clock.ChannelId == cid && clock.TzId == tzid) == null)
            {
                var clockConfig = new ClockConfiguration
                {
                    GuildId = gid,
                    ChannelId = cid,
                    TzId = tzid
                };
                db.ClockData.Add(clockConfig);
                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Deletes a clock configuration.
        /// </summary>
        public static async Task DeleteClock(ulong gid, ulong cid)
        {
            using var db = new ConfigurationContext();
            ClockConfiguration exists = await db.ClockData.SingleAsync(clock => clock.GuildId == gid & clock.ChannelId == cid);
            if (exists != null)
            {
                db.ClockData.Remove(exists);
                await db.SaveChangesAsync();
            }
        }

        private void BuildConfiguration()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("config.json", false, true)
                .Build();
        }
    }
}
