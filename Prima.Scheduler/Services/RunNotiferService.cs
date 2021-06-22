using DSharpPlus;
using DSharpPlus.Entities;
using Prima.Models;
using Prima.Resources;
using Prima.Services;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prima.Scheduler.Services
{
    public class RunNotiferService : IDisposable
    {
        private const ulong HostRole = 762072215356702741;
        private const long Threshold = 1800000;
        private const int CheckInterval =
#if DEBUG
                    1000
#else
                    300000
#endif
            ;

        private readonly CancellationTokenSource _tokenSource;
        private readonly IDbService _db;
        private readonly DiscordClient _client;

        public RunNotiferService(IDbService db, DiscordClient client)
        {
            _tokenSource = new CancellationTokenSource();
            _db = db;
            _client = client;
        }

        public void Initialize()
        {
            Task.Run(() => NotificationLoop(_tokenSource.Token));
        }

        private async Task NotificationLoop(CancellationToken token)
        {
            var tzi = TimeZoneInfo.FindSystemTimeZoneById(Util.PstIdString());

            while (true)
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                var runs = _db.Events.Where(e => DateTime.FromBinary(e.RunTime) > DateTime.Now && !e.Notified);
                foreach (var run in runs)
                {
                    var timeDiff = (DateTime.FromBinary(run.RunTime) - DateTime.Now).TotalMilliseconds;
#if DEBUG
                    Log.Debug(timeDiff.ToString(CultureInfo.InvariantCulture));
#endif

                    if (timeDiff < Threshold)
                    {
                        if (timeDiff < 0)
                        {
                            run.Notified = true;
                            await _db.UpdateScheduledEvent(run);
                            continue;
                        }

                        Log.Information("Run {MessageId}, notifications started.", run.MessageId3);

                        var guild = await _client.GetGuildAsync(run.GuildId);
                        if (guild == null)
                        {
                            Log.Error("No guild found, skipping!");
                            continue;
                        }

                        var guildConfig = _db.Guilds.FirstOrDefault(gc => gc.Id == guild.Id);
                        if (guildConfig == null)
                        {
                            Log.Error("No guild configuration found, skipping!");
                            continue;
                        }
                        var commandChannel = guild.GetChannel(guildConfig.ScheduleInputChannel);
                        var outputChannel = guild.GetChannel(guildConfig.ScheduleOutputChannel);

                        var leader = await guild.GetMemberAsync(run.LeaderId);
                        try
                        {
                            Log.Information("Sending notification to leader.");
                            await leader.SendMessageAsync("The run you scheduled is set to begin in 30 minutes!\n\n" +
                                $"Message link: <{(await commandChannel.GetMessageAsync(run.MessageId3)).JumpLink}>");
                        }
                        catch (Exception)
                        {
                            try
                            {
                                var message = await commandChannel.SendMessageAsync($"{leader.Mention}, the run you scheduled is set to begin in 30 minutes!\n\n" +
                                    $"Message link: <{(await commandChannel.GetMessageAsync(run.MessageId3)).JumpLink}>");
                                (new Task(async () =>
                                {
                                    await Task.Delay((int)Threshold, token);
                                    try
                                    {
                                        await message.DeleteAsync();
                                    }
                                    catch (Exception) { } // Message was already deleted.
                                })).Start();
                            }
                            catch (Exception)
                            {
                                Log.Warning("Every attempt at notifying {LeaderName} failed.", leader.ToString());
                            }
                        }

                        foreach (var userId in run.SubscribedUsers)
                        {
                            var member = await guild.GetMemberAsync(ulong.Parse(userId));
                            await TryNotifyMember(member, leader, commandChannel, run, guildConfig, token);
                        }

                        run.Notified = true;
                        await _db.UpdateScheduledEvent(run);

                        var embedMessage = await outputChannel.GetMessageAsync(run.EmbedMessageId);
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay((int)Threshold, token);
                            await AssignHost(leader);
                            await embedMessage.DeleteAsync();
                        }, token);
                    }
                }

                await Task.Delay(CheckInterval, token);
            }
        }

        private static async Task TryNotifyMember(DiscordMember member, DiscordMember leader, DiscordChannel commandChannel, ScheduledEvent @event, DiscordGuildConfiguration guildConfig, CancellationToken token)
        {
            var success = false;
            try
            {
                await member.SendMessageAsync(
                    $"The run you reacted to (hosted by {leader.Nickname ?? leader.Username}) is beginning in 30 minutes!\n\n" +
                    $"Message link: <{(await commandChannel.GetMessageAsync(@event.MessageId3)).JumpLink}>");
                success = true;
            }
            catch (Exception)
            {
                try
                {
                    var message = await commandChannel.SendMessageAsync($"{member.Mention}, the run you reacted to (hosted by {leader.Nickname ?? leader.Username}) is beginning in 30 minutes!\n\n" +
                        $"Message link: <{(await commandChannel.GetMessageAsync(@event.MessageId3)).JumpLink}>");
                    (new Task(async () =>
                    {
                        await Task.Delay((int)Threshold, token);
                        try
                        {
                            await message.DeleteAsync();
                        }
                        catch (Exception) { } // Message was already deleted.
                    })).Start();
                    success = true;
                }
                catch (Exception)
                {
                    Log.Warning("Every attempt at message user {Username} failed.", member.ToString());
                }
            }
            if (success) Log.Information($"Info sent to {member} about {leader}'s run.");
        }

        private async Task AssignHost(DiscordMember host)
        {
            var guild = host.Guild;

            var hostRole = guild.GetRole(HostRole);
            var runPinner = guild.GetRole(RunHostData.PinnerRoleId);

            await host.GrantRolesAsync(new[] { hostRole, runPinner });

            await _db.AddTimedRole(HostRole, guild.Id, host.Id, DateTime.UtcNow.AddHours(4.5));
            await _db.AddTimedRole(runPinner.Id, guild.Id, host.Id, DateTime.UtcNow.AddHours(4.5));
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
