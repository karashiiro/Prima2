using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Prima.Resources;
using Prima.Services;
using Serilog;

namespace Prima.Scheduler.Services
{
    public class AnnounceMonitor : IDisposable
    {
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;

        private readonly CancellationTokenSource _tokenSource;

        public AnnounceMonitor(DbService db, DiscordSocketClient client)
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
            var guildConfig = _db.Guilds.FirstOrDefault(g => g.Id == SpecialGuilds.CrystalExploratoryMissions);
            if (guildConfig == null)
            {
                Log.Error("No guild configuration found for the default guild!");
                return;
            }

            var guild = _client.GetGuild(guildConfig.Id);
            var channel = guild.GetTextChannel(guildConfig.DelubrumScheduleOutputChannel);

            var executor = guild.GetRole(DelubrumProgressionRoles.Executor);
            var currentHost = guild.GetRole(RunHostData.RoleId);

            var tzi = TimeZoneInfo.FindSystemTimeZoneById(Util.PstIdString());

            while (!token.IsCancellationRequested)
            {
                await foreach (var page in channel.GetMessagesAsync().WithCancellation(token))
                {
                    foreach (var message in page)
                    {
                        var embed = message.Embeds.FirstOrDefault(e => e.Type == EmbedType.Rich);

                        var nullableTimestamp = embed?.Timestamp;
                        if (!nullableTimestamp.HasValue) continue;

                        var timestamp = nullableTimestamp.Value.AddHours(tzi.BaseUtcOffset.Hours);
                        if (timestamp.AddMinutes(30) >= DateTimeOffset.Now && embed.Author.HasValue)
                        {
                            var host = guild.Users.FirstOrDefault(u => u.ToString() == embed.Author.Value.Name);
                            if (host == null)
                            {
                                await guild.DownloadUsersAsync();
                                host = guild.Users.FirstOrDefault(u => u.ToString() == embed.Author.Value.Name);
                            }

                            if (host != null)
                            {
                                await host.AddRoleAsync(executor);
                                await host.AddRoleAsync(currentHost);
                                await host.SendMessageAsync(
                                    "You have been given the Delubrum Host role for 3 1/2 hours!\n" +
                                    "You can now use the command `~setroler @User` to give them access to the progression " +
                                    "role commands `~addprogrole @User @Role` and `~removeprogrole @User @Role`!");
                                _ = Task.Run(async () =>
                                {
                                    await Task.Delay(new TimeSpan(3, 30, 0), token);
                                    await host.RemoveRoleAsync(executor);
                                    await host.RemoveRoleAsync(currentHost);
                                }, token);
                            }
                        }
                    }
                }

                await Task.Delay(new TimeSpan(0, 5, 0), token);
            }
        }

        private bool _disposed;
        public void Dispose()
        {
            if (_disposed)
                return;
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
            _disposed = true;
        }
    }
}