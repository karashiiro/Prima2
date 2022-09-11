using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Prima.Services;
using Serilog;

namespace Prima.DiscordNet.Services
{
    public class TimedRoleManager : IDisposable
    {
        private readonly IDbService _db;
        private readonly DiscordSocketClient _client;

        private readonly CancellationTokenSource _tokenSource;

        public TimedRoleManager(DiscordSocketClient client, IDbService db)
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
                var toRemove = await _db.TimedRoles
                    .Where(tr => tr.RemovalTime <= DateTime.UtcNow)
                    .ToListAsync(token);
                if (toRemove.Any())
                {
                    Log.Information("Removing {RoleCount} roles...", toRemove.Count);

                    var failCount = 0;
                    foreach (var tr in toRemove)
                    {
                        var guild = _client.GetGuild(tr.GuildId);
                        var role = guild.GetRole(tr.RoleId);

                        var user = await _client.GetUserAsync(tr.UserId);
                        var member = guild.GetUser(user.Id);
                        if (member == null)
                        {
                            failCount++;
                            continue;
                        }

                        Log.Information("Removing role {Role} from {User}.", role.Name, member.ToString());
                        try
                        {
                            await member.RemoveRoleAsync(role);
                        }
                        catch { /* ignored */ }
                        await _db.RemoveTimedRole(tr.RoleId, tr.UserId);
                    }

                    if (failCount != 0)
                    {
                        Log.Information("Failed to remove {FailCount} roles.", failCount);
                    }
                }

#if DEBUG
                await Task.Delay(1000, token).ConfigureAwait(false);
#else
                await Task.Delay(new TimeSpan(0, 5, 0), token).ConfigureAwait(false);
#endif
            }
        }

        private bool _disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;
            if (!disposing) return;

            _tokenSource?.Cancel();
            _tokenSource?.Dispose();

            _disposedValue = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}