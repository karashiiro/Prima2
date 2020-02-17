using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Prima.Contexts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Prima.Services
{
    public class ConfigurationService
    {
        /// <summary>
        /// Gets the current <see cref="Preset"/> of the bot.
        /// </summary>
        public Preset CurrentPreset { get; private set; }

        /// <summary>
        /// Gets the directory of the temporary cache.
        /// </summary>
        public string TempDir { get => Util.GetAbsolutePath(GetSection("TemporaryCache").Value); }

        /// <summary>
        /// Gets the directory of the queue folder.
        /// </summary>
        public string QueueDir { get => Util.GetAbsolutePath(GetSection("QueueFolder").Value); }

        /// <summary>
        /// Gets the directory of the calendar information folder.
        /// </summary>
        public string CalendarDir { get => Util.GetAbsolutePath(GetSection("CalendarFolder").Value); }

        /// <summary>
        /// Gets the directory of the Google APIs token.json.
        /// </summary>
        public string GTokenFile { get => Util.GetAbsolutePath(GetSection("GoogleToken").Value); }

        private IConfigurationRoot _config;
        public ConfigurationService(Preset preset)
        {
            CurrentPreset = preset;
            BuildConfiguration();
            try
            {
                Directory.CreateDirectory(TempDir);
            }
            catch (IOException) {}
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
            IList<ClockConfiguration> configurations = new List<ClockConfiguration>();
            using var db = new ConfigurationContext();
            try
            {
                configurations = await db.ClockData.ToListAsync();
            }
            catch (SqliteException) {}
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
            try
            {
                ClockConfiguration cc = await db.ClockData.SingleAsync(clock => clock.GuildId == gid & clock.ChannelId == cid);
                db.ClockData.Remove(cc);
                await db.SaveChangesAsync();
            }
            catch (InvalidOperationException) {}
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
