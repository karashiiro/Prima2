using Discord.WebSocket;
using NodaTime;
using Prima.Contexts;
using Prima.Resources;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prima.Services
{
    public class ServerClockService
    {
        private readonly DiscordSocketClient _client;
        private readonly List<Clock> _clocks;
        private readonly SystemClock _systemClock;

        private Task _runningTask;
        private bool _active;

        public ServerClockService(DiscordSocketClient client, SystemClock systemClock)
        {
            _client = client;
            _clocks = new List<Clock>();
            _systemClock = systemClock;
        }

        public async Task InitializeAsync()
        {
            IList<ClockConfiguration> clockConfigurations = await ConfigurationService.GetClockData();
            foreach (var cc in clockConfigurations)
            {
                await AddClock(cc.GuildId, cc.ChannelId, cc.TzId);
            }
        }

        public void Start()
        {
            if (_active) return;
            _active = true;
            _runningTask = Tick();
        }

        public void Stop()
        {
            _active = false;
        }

        /// <summary>
        /// Adds a clock to the queue, replacing one on the provided channel if it already exists.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task AddClock(ulong guildId, ulong channelId, string tz)
        {
            if (_clocks.Exists(clock => clock.ChannelId == channelId))
            {
                await RemoveClock(channelId);
            }

            var timezone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(tz);
            var newClock = new ZonedClock(_systemClock, timezone, CalendarSystem.Iso);
            _clocks.Add(new Clock
            {
                GuildId = guildId,
                ChannelId = channelId,
                Timezone = newClock
            });

            await ConfigurationService.SaveClock(guildId, channelId, tz);
        }

        /// <summary>
        /// Removes a clock from the queue.
        /// </summary>
        public async Task RemoveClock(ulong channelId)
        {
            Clock c = _clocks.Find(clock => clock.ChannelId == channelId);
            await ConfigurationService.DeleteClock(c.GuildId, c.ChannelId);
            _clocks.Remove(c);
        }

        public bool IsFaulted() => _runningTask.IsFaulted;

        private async Task Tick()
        {
            while (_active)
            {
                foreach (Clock c in _clocks)
                {
                    SocketGuildChannel channel = _client.GetGuild(c.GuildId).GetChannel(c.ChannelId);
                    await channel.ModifyAsync(properties =>
                    {
                        ZonedDateTime time = c.Timezone.GetCurrentZonedDateTime();
                        string clockEmoji = time.Minute < 30 ? Arrays.ClockFaces[time.Hour / 2] : Arrays.HalfHourClockFaces[time.Hour / 2];
                        properties.Name = clockEmoji + " " + time.ToString("h:mm tt z", null);
                        Log.Information("Done!");
                    });
                }
                await Task.Delay(60000);
            }
        }

        private struct Clock
        {
            public ulong GuildId { get; set; }
            public ulong ChannelId { get; set; }
            public ZonedClock Timezone { get; set; }
        }
    }
}
