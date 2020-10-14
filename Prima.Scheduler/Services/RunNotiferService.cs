using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Prima.Models;
using Prima.Resources;
using Prima.Services;
using Serilog;

namespace Prima.Scheduler.Services
{
    public class RunNotiferService : IDisposable
    {
        private const long Threshold = 1800000;
        private const int CheckInterval = 300000;

        private readonly CancellationTokenSource _tokenSource;
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;

        public RunNotiferService(DbService db, DiscordSocketClient client)
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
            while (true)
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                Log.Information("{RunCount} scheduled runs.", _db.Events.Count());
                var runs = _db.Events.Where(e => e.RunTime > DateTime.Now.ToBinary() && !e.Notified);
                foreach (var run in runs)
                {
                    var timeDiff = (DateTime.FromBinary(run.RunTime) - DateTime.Now).TotalMilliseconds;

                    if (timeDiff < Threshold)
                    {
                        if (timeDiff < 0)
                        {
                            run.Notified = true;
                            await _db.UpdateScheduledEvent(run);
                            continue;
                        }

                        Log.Information("Run {MessageId}, notifications started.", run.MessageId3);

                        var guild = _client.Guilds.FirstOrDefault(g => g.Id == run.GuildId);
                        if (guild == null)
                            continue;
                        await guild.DownloadUsersAsync();

                        var guildConfig = _db.Guilds.FirstOrDefault(gc => gc.Id == guild.Id);
                        if (guildConfig == null) continue;
                        var commandChannel = guild.GetTextChannel(run.RunKindCastrum == RunDisplayTypeCastrum.None ? guildConfig.ScheduleInputChannel : guildConfig.CastrumScheduleInputChannel);
                        var outputChannel = guild.GetTextChannel(run.RunKindCastrum == RunDisplayTypeCastrum.None ? guildConfig.ScheduleOutputChannel : guildConfig.CastrumScheduleOutputChannel);

                        var leader = guild.GetUser(run.LeaderId);
                        try
                        {
                            await leader.SendMessageAsync("The run you scheduled is set to begin in 30 minutes!\n\n" +
                                $"Message link: <{(await commandChannel.GetMessageAsync(run.MessageId3)).GetJumpUrl()}>");
                        }
                        catch (HttpException)
                        {
                            try
                            {
                                var message = await commandChannel.SendMessageAsync($"{leader.Mention}, the run you scheduled is set to begin in 30 minutes!\n\n" +
                                    $"Message link: <{(await commandChannel.GetMessageAsync(run.MessageId3)).GetJumpUrl()}>");
                                (new Task(async () => {
                                    await Task.Delay((int)Threshold, token);
                                    try
                                    {
                                        await message.DeleteAsync();
                                    }
                                    catch (HttpException) {} // Message was already deleted.
                                })).Start();
                            }
                            catch (HttpException)
                            {
                                Log.Warning("Every attempt at notifying {LeaderName} failed.", leader.ToString());
                            }
                        }

                        foreach (var userId in run.SubscribedUsers)
                        {
                            var member = guild.GetUser(ulong.Parse(userId));
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
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(1000 * 60 * 60 * 3, token);
                            await UnassignHost(leader);
                        }, token);
                    }
                }

                await Task.Delay(CheckInterval, token);
            }
        }

        private async Task TryNotifyMember(IUser member, IGuildUser leader, ISocketMessageChannel commandChannel, ScheduledEvent @event, DiscordGuildConfiguration guildConfig, CancellationToken token)
        {
            var success = false;
            try
            {
                await member.SendMessageAsync(
                    $"The run you reacted to (hosted by {leader.Nickname ?? leader.Username}) is beginning in 30 minutes!\n\n" +
                    $"Message link: <{(await commandChannel.GetMessageAsync(@event.MessageId3)).GetJumpUrl()}>");
                success = true;
            }
            catch (HttpException)
            {
                try
                {
                    var message = await commandChannel.SendMessageAsync($"{member.Mention}, the run you reacted to (hosted by {leader.Nickname ?? leader.Username}) is beginning in 30 minutes!\n\n" +
                        $"Message link: <{(await commandChannel.GetMessageAsync(@event.MessageId3)).GetJumpUrl()}>");
                    (new Task(async () => {
                        await Task.Delay((int)Threshold, token);
                        try
                        {
                            await message.DeleteAsync();
                        }
                        catch (HttpException) {} // Message was already deleted.
                    })).Start();
                    success = true;
                }
                catch (HttpException)
                {
                    Log.Warning("Every attempt at message user {Username} failed.", member.ToString());
                }
            }
            if (success) Log.Information($"Info sent to {member} about {leader}'s run.");
        }

        private static async Task AssignHost(IGuildUser host)
        {
            var hostRole = host.Guild.GetRole(762072215356702741);
            await host.AddRoleAsync(hostRole);
        }

        private static async Task UnassignHost(IGuildUser host)
        {
            var hostRole = host.Guild.GetRole(762072215356702741);
            try
            {
                await host.RemoveRoleAsync(hostRole);
            }
            catch
            {
                await Task.Delay(5000);
                await host.RemoveRoleAsync(hostRole);
            }
        }

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            _tokenSource.Cancel();
            _tokenSource.Dispose();
            _disposed = true;
        }
    }
}
