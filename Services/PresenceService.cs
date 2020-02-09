using Discord.WebSocket;
using Prima.Resources;
using System;
using System.Threading.Tasks;

using Activity = System.Collections.Generic.KeyValuePair<string, Discord.ActivityType>;

namespace Prima.Services
{
    public class PresenceService
    {
        public int DelayTime { get; private set; }

        private readonly DiscordSocketClient _client;

        private Task _runningTask;
        private bool _active;
        
        public PresenceService(DiscordSocketClient client)
        {
            _client = client;

            DelayTime = 900000;
        }

        public void Start()
        {
            if (_active) return;
            _active = true;
            _runningTask = StartPresenceTask();
        }

        public void Stop() => _active = false;

        public void SetDelay(int ms) => DelayTime = ms;

        public bool IsFaulted() => _runningTask.IsFaulted;

        public async Task NextPresence()
        {
            Activity presence = Presences.List[(new Random()).Next(0, Presences.List.Length)];
            await _client.SetGameAsync(presence.Key, null, presence.Value);
        }

        private async Task StartPresenceTask()
        {
            while (_active)
            {
                await NextPresence();
                await Task.Delay(DelayTime);
            }
        }
    }
}
