using System;
using System.Threading;
using Discord.WebSocket;
using Prima.Stable.Resources;

namespace Prima.Stable.Services
{
    public class PresenceService : IDisposable
    {
        public int DelayTime { get; private set; }

        private readonly DiscordSocketClient _client;

        private Thread _loopThread;
        private volatile bool _active;
        
        public PresenceService(DiscordSocketClient client)
        {
            _client = client;
            DelayTime = 900000;
            Start();
        }

        private void Start()
        {
            if (_active) return;
            _active = true;
            _loopThread = StartPresenceLoop();
        }

        private void Stop()
        {
            if (!_active) return;
            _active = false;
            _loopThread.Join();
            _loopThread = null;
        }

        /// <summary>
        /// Sets the presence update delay for the presence loop.
        /// </summary>
        public void SetDelay(int ms)
        {
            DelayTime = ms;
            Stop();
            Start();
        }

        /// <summary>
        /// Advances to the next presence.
        /// </summary>
        public void NextPresence()
        {
            var (name, activityType) = Presences.List[new Random().Next(0, Presences.List.Length)];
            _ = _client.SetGameAsync(name, null, activityType);
        }

        private Thread StartPresenceLoop()
        {
            return new(() =>
            {
                while (_active)
                {
                    NextPresence();
                    Thread.Sleep(DelayTime);
                }
            });
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
