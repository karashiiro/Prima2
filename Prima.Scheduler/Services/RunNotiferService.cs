using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Prima.Models;
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
                    if ((DateTime.FromBinary(run.RunTime) - DateTime.Now).TotalMilliseconds < Threshold) // I have no clue why this is necessary to do this way
                    {
                        Log.Information("Run {MessageId}, notifications started.", run.MessageId3);

                        var guild = _client.Guilds.FirstOrDefault(g => g.Id == run.GuildId);
                        if (guild == null)
                            continue;
                        await guild.DownloadUsersAsync();

                        var guildConfig = _db.Guilds.FirstOrDefault(gc => gc.Id == guild.Id);
                        if (guildConfig == null) continue;
                        var commandChannel = guild.GetTextChannel(guildConfig.ScheduleInputChannel);
                        var outputChannel = guild.GetTextChannel(guildConfig.ScheduleOutputChannel);

                        var leader = guild.GetUser(run.LeaderId);
                        try
                        {
                            await leader.SendMessageAsync("The run you scheduled is set to begin in 30 minutes!");
                        }
                        catch (HttpException)
                        {
                            try
                            {
                                await commandChannel.SendMessageAsync(
                                    $"{leader.Mention}, the run you scheduled is set to begin in 30 minutes!");
                            }
                            catch (HttpException)
                            {
                                Log.Warning("Every attempt at notifying {LeaderName} failed.", leader.ToString());
                            }
                        }

                        foreach (var userId in run.SubscribedUsers)
                        {
                            var member = guild.GetUser(ulong.Parse(userId));
                            await TryNotifyMember(member, leader, commandChannel);
                        }

                        run.Notified = true;
                        await _db.UpdateScheduledEvent(run);

                        var embedMessage = await outputChannel.GetMessageAsync(run.EmbedMessageId);
                        _ = Task.Run(async () => // Delete embed 30 minutes later
                        {
                            await Task.Delay((int)Threshold, token);
                            await embedMessage.DeleteAsync();
                        }, token);
                    }
                }

                await Task.Delay(CheckInterval, token);
            }
        }

        private static async Task TryNotifyMember(IUser member, IGuildUser leader, ISocketMessageChannel commandChannel)
        {
            var success = false;
            try
            {
                await member.SendMessageAsync(
                    $"The run you reacted to (hosted by {leader.Nickname ?? leader.Username}) is beginning in 30 minutes!");
                success = true;
            }
            catch (HttpException)
            {
                try
                {
                    await commandChannel.SendMessageAsync($"{member.Mention}, the run you reacted to (hosted by {leader.Nickname ?? leader.Username}) is beginning in 30 minutes!");
                    success = true;
                }
                catch (HttpException)
                {
                    Log.Warning("Every attempt at message user {Username} failed.", member.ToString());
                }
            }
            if (success) Log.Information($"Info sent to {member} about {leader}'s run.");
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
