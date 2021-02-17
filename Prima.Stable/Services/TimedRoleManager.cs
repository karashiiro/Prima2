using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Prima.Services;

namespace Prima.Stable.Services
{
    public class TimedRoleManager : IDisposable
    {
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;

        private readonly CancellationTokenSource _tokenSource;
        
        public TimedRoleManager(DiscordSocketClient client, DbService db)
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
                var toRemove = _db.TimedRoles.Where(tr => tr.RemovalTime <= DateTime.UtcNow);
                await foreach (var tr in toRemove.WithCancellation(token))
                {
                    var guild = _client.GetGuild(tr.GuildId);
                    var role = guild.GetRole(tr.RoleId);
                    var user = guild.GetUser(tr.UserId);
                    try
                    {
                        await user.RemoveRoleAsync(role);
                    }
                    catch { /* ignored */ }
                    await _db.RemoveTimedRole(tr.RoleId, tr.UserId);
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