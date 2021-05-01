using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Prima.Services;
using Serilog;

namespace Prima.Stable.Services
{
    public class EphemeralPinManager
    {
        private readonly IDbService _db;
        private readonly DiscordSocketClient _client;

        private readonly CancellationTokenSource _tokenSource;

        public EphemeralPinManager(DiscordSocketClient client, IDbService db)
        {
            _db = db;
            _client = client;
            _tokenSource = new CancellationTokenSource();
        }

        public void Initialize()
        {
            Task.Run(() => CheckLoop(_tokenSource.Token));
        }

        private async Task CheckLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var toRemove = await _db.EphemeralPins
                    .Where(tr => tr.PinTime.AddHours(4.5) <= DateTime.UtcNow)
                    .ToListAsync(token);
                if (toRemove.Any())
                {
                    Log.Information("Removing {MessageCount} pinned messages...", toRemove.Count);
                    foreach (var e in toRemove)
                    {
                        var guild = _client.GetGuild(e.GuildId);
                        var channel = guild?.GetTextChannel(e.ChannelId);
                        if (channel == null)
                        {
                            Log.Warning("Could not access channel {ChannelId}, skipping...", e.ChannelId);
                            continue;
                        }

                        var message = await channel.GetMessageAsync(e.MessageId) as IUserMessage;
                        Log.Information("Removing pinned message {MessageId}.", e.MessageId);
                        try
                        {
                            var task = message?.UnpinAsync();
                            if (task != null)
                            {
                                await task;
                            }
                        }
                        catch { /* ignored */ }
                        await _db.RemoveEphemeralPin(e.MessageId);
                    }
                }

#if DEBUG
                await Task.Delay(1000, token);
#else
                await Task.Delay(new TimeSpan(0, 5, 0), token);
#endif
            }
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) return;
            if (!disposing) return;

            _tokenSource?.Cancel();
            _tokenSource?.Dispose();

            disposedValue = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}