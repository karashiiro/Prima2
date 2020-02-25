using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NodaTime;
using Prima.Contexts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Prima.Services
{
    public sealed class ConfigurationService : IDisposable
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

        private readonly ConfigurationContext _configStore;
        private IConfigurationRoot _config;

        public ConfigurationService(Preset preset)
        {
            CurrentPreset = preset;
            _configStore = new ConfigurationContext();
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
        /// Gets a configuration sub-section with the specified key as a <see cref="byte"/>.
        /// </summary>
        public byte GetByte(string key)
        {
            IConfigurationSection section = GetSection(key);
            return byte.Parse(section.Value);
        }

        /// <summary>
        /// Gets a configuration sub-section with the specified key as a <see cref="byte"/>.
        /// </summary>
        public byte GetByte(params string[] keys)
        {
            IConfigurationSection section = GetSection(keys);
            return byte.Parse(section.Value);
        }

        /// <summary>
        /// Get the stored clock configurations.
        /// </summary>
        public async Task<IList<ClockConfiguration>> GetClockData()
        {
            IList<ClockConfiguration> configurations = new List<ClockConfiguration>();
            configurations = await _configStore.ClockData.ToListAsync();
            return configurations;
        }

        /// <summary>
        /// Save a clock configuration.
        /// </summary>
        public async Task SaveClock(ulong gid, ulong cid, ZonedClock timezone)
        {
            try
            {
                var existing = await _configStore.ClockData.SingleAsync(clock => clock.ChannelId == cid);
                _configStore.Remove(existing);
            }
            catch (InvalidOperationException) {}
            var clockConfig = new ClockConfiguration
            {
                GuildId = gid,
                ChannelId = cid,
                Timezone = timezone
            };
            _configStore.ClockData.Add(clockConfig);
            await _configStore.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a clock configuration.
        /// </summary>
        public async Task DeleteClock(ulong cid)
        {
            try
            {
                ClockConfiguration cc = await _configStore.ClockData.SingleAsync(clock => clock.ChannelId == cid);
                _configStore.ClockData.Remove(cc);
                await _configStore.SaveChangesAsync();
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

        public void Dispose()
        {
            _configStore.Dispose();
        }
    }
}
