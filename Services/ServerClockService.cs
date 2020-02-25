using Discord.WebSocket;
using NodaTime;
using Prima.Resources;
using Serilog;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Prima.Services
{
    public class ServerClockService
    {
        private readonly ConfigurationService _config;
        private readonly DiscordSocketClient _client;
        private readonly SystemClock _systemClock;

        private Task _runningTask;
        private bool _active;

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
        public ServerClockService(ConfigurationService config, DiscordSocketClient client, SystemClock systemClock)
        {
            _config = config;
            _client = client;
            _systemClock = systemClock;
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
            var timezone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(tz);
            var newClock = new ZonedClock(_systemClock, timezone, CalendarSystem.Iso);

            await _config.SaveClock(guildId, channelId, newClock);
        }

        /// <summary>
        /// Removes a clock from the queue.
        /// </summary>
        public async Task RemoveClock(ulong channelId)
        {
            await _config.DeleteClock(channelId);
        }

        public bool IsFaulted() => _runningTask.IsFaulted;

        private async Task Tick()
        {
            while (_active)
            {
                var clocks = await _config.GetClockData();
                foreach (var c in clocks)
                {
                    SocketGuildChannel channel = _client.GetGuild(c.GuildId).GetChannel(c.ChannelId);
                    await channel.ModifyAsync(properties =>
                    {
                        ZonedDateTime time = c.Timezone.GetCurrentZonedDateTime();
                        string clockEmoji = time.Minute < 30 ? Arrays.ClockFaces[time.Hour / 2] : Arrays.HalfHourClockFaces[time.Hour / 2];
                        properties.Name = clockEmoji + " " + time.ToString("h:mm tt x", null);
                        Log.Information("Done!");
                    });
                }
                await Task.Delay(60000);
            }
        }
    }
}
