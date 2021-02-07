using Discord;
using Discord.WebSocket;
using Prima.Resources;
using Prima.Services;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Net;

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

            while (!token.IsCancellationRequested)
            {
                var guild = _client.GetGuild(guildConfig.Id);

                var drsCheck = CheckRuns(guild, guildConfig.DelubrumScheduleOutputChannel, async (host, embedMessage, embed) =>
                {
                    var success = await AssignHostRole(guild, host, token);
                    if (!success) return;

                    await AssignExecutorRole(guild, host, token);
                    await NotifyMembers(host, embedMessage, token);
                    await host.SendMessageAsync(
                        "You have been given the Delubrum Host role for 3 1/2 hours!\n" +
                        "You can now use the command `~setroler @User` to give them access to the progression " +
                        "role commands `~addprogrole @User Role Name` and `~removeprogrole @User Role Name`!\n" +
                        "You can also modify multiple users at once by using `~addprogrole @User1 @User2 Role Name`.\n\n" +
                        "Available roles:\n" +
                        "▫️ Trinity Seeker Progression\n" +
                        "▫️ Dahu Progression\n" +
                        "▫️ Queen's Guard Progression\n" +
                        "▫️ Phantom Progression\n" +
                        "▫️ Trinity Avowed Progression\n" +
                        "▫️ Stygimoloch Lord Progression\n" +
                        "▫️ The Queen Progression");
                }, token);

                var drnCheck = CheckRuns(guild, guildConfig.DelubrumNormalScheduleOutputChannel, async (host, embedMessage, embed) =>
                {
                    var success = await AssignHostRole(guild, host, token);
                    if (!success) return;

                    await NotifyLead(host);
                    await NotifyMembers(host, embedMessage, token);
                }, token);

                var castrumCheck = CheckRuns(guild, guildConfig.CastrumScheduleOutputChannel, async (host, embedMessage, embed) =>
                {
                    var success = await AssignHostRole(guild, host, token);
                    if (!success) return;

                    await NotifyLead(host);
                    await NotifyMembers(host, embedMessage, token);
                }, token);

                await Task.WhenAll(drsCheck, drnCheck, castrumCheck);
#if DEBUG
                await Task.Delay(1000, token);
#else
                await Task.Delay(new TimeSpan(0, 5, 0), token);
#endif
            }
        }

        private static async Task CheckRuns(SocketGuild guild, ulong channelId, Func<SocketGuildUser, IMessage, IEmbed, Task> onMatch, CancellationToken token)
        {
            var channel = guild?.GetTextChannel(channelId);
            if (channel == null)
            {
                await Task.Delay(3000, token);
                return;
            }

            await foreach (var page in channel.GetMessagesAsync().WithCancellation(token))
            {
                foreach (var message in page)
                {
                    var embed = message.Embeds.FirstOrDefault(e => e.Type == EmbedType.Rich);

                    var nullableTimestamp = embed?.Timestamp;
                    if (!nullableTimestamp.HasValue) continue;

                    var timestamp = nullableTimestamp.Value;
                    Log.Information("Current timestamp: {CurrentTimestamp}; announcement timestamp: {Timestamp}", DateTimeOffset.Now.ToString(), timestamp.ToString());

                    // Remove expired posts
                    if (timestamp.AddMinutes(15) < DateTimeOffset.Now)
                    {
                        await message.DeleteAsync();
                        continue;
                    }

                    // ReSharper disable once InvertIf
                    if (timestamp.AddMinutes(-30) <= DateTimeOffset.Now && embed.Author.HasValue)
                    {
                        Log.Information("Run matched!");

                        var host = guild.Users.FirstOrDefault(u => u.ToString() == embed.Author.Value.Name);
                        if (host == null)
                        {
                            await guild.DownloadUsersAsync();
                            host = guild.Users.FirstOrDefault(u => u.ToString() == embed.Author.Value.Name);
                        }

                        var messageReferenceCopy = message;
                        try
                        {
                            await onMatch(host, messageReferenceCopy, embed);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e, "error: uncaught exception in onMatch");
                        }
                    }
                }
            }
        }

        private static async Task<bool> AssignHostRole(SocketGuild guild, SocketGuildUser host, CancellationToken token)
        {
            var currentHost = guild.GetRole(RunHostData.RoleId);

            Log.Information("Assigning roles...");
            if (host != null && !host.HasRole(currentHost))
            {
                await host.AddRoleAsync(currentHost);
                _ = Task.Run(async () =>
                {
                    await Task.Delay(new TimeSpan(3, 30, 0), token);
                    await host.RemoveRoleAsync(currentHost);
                }, token);

                return true;
            }

            return false;
        }

        private static async Task AssignExecutorRole(SocketGuild guild, IGuildUser host, CancellationToken token)
        {
            var executor = guild.GetRole(DelubrumProgressionRoles.Executor);
            await host.AddRoleAsync(executor);
            _ = Task.Run(async () =>
            {
                await Task.Delay(new TimeSpan(3, 30, 0), token);
                await host.RemoveRoleAsync(executor);
            }, token);
        }

        private static async Task NotifyLead(IUser host)
        {
            try
            {
                await host.SendMessageAsync("The run you scheduled is set to begin in 30 minutes!");
            }
            catch (HttpException e) when (e.DiscordCode == 50007)
            {
                Log.Warning("Can't send direct message to user {User}.", host.ToString());
            }
        }

        private async Task NotifyMembers(IGuildUser host, IMessage embedMessage, CancellationToken token)
        {
            Log.Information("Notifying reactors... {Count}", embedMessage.Reactions.Count);

            var (nrEmote, nrMeta) = embedMessage.Reactions
                .FirstOrDefault(erm => erm.Key.Name == "📳");

            if (nrEmote == null) return;

            await foreach (var page in embedMessage.GetReactionUsersAsync(nrEmote, nrMeta.ReactionCount).WithCancellation(token))
            {
                foreach (var user in page)
                {
                    
                    if (user.IsBot) continue;

                    try
                    {
                        await user.SendMessageAsync($"The run you reacted to (hosted by {host.Nickname ?? host.Username}) is beginning in 30 minutes!");
                    }
                    catch (HttpException e) when (e.DiscordCode == 50007)
                    {
                        Log.Warning("Can't send direct message to user {User}.", host.ToString());
                    }
                }
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