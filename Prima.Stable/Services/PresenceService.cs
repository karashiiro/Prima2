using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Prima.Stable.Resources;

namespace Prima.Stable.Services
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

        public void SetDelay(int ms)
        {
            DelayTime = ms;
            Stop();
            Start();
        }

        public bool IsFaulted() => _runningTask.IsFaulted;

        public Task NextPresence()
        {
            var (name, activityType) = Presences.List[(new Random()).Next(0, Presences.List.Length)];
            return _client.SetGameAsync(name, null, activityType);
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
