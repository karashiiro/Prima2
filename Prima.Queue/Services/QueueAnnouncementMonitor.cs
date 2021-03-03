using System;
using System.Collections.Generic;
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

namespace Prima.Queue.Services
{
    public class QueueAnnouncementMonitor : IDisposable
    {
        private readonly IDbService _db;
        private readonly DiscordSocketClient _client;
        private readonly FFXIV3RoleQueueService _queueService;

        private readonly CancellationTokenSource _tokenSource;

        public QueueAnnouncementMonitor(IDbService db, DiscordSocketClient client, FFXIV3RoleQueueService queueService)
        {
            _db = db;
            _client = client;
            _queueService = queueService;

            _tokenSource = new CancellationTokenSource();
        }

        public void Initialize()
        {
            Task.Run(() => CheckLoop(_tokenSource.Token));
        }

        private async Task CheckLoop(CancellationToken token)
        {
            Log.Information("Starting queue announcement check loop.");

            var guildConfig = _db.Guilds.FirstOrDefault(g => g.Id == SpecialGuilds.CrystalExploratoryMissions);
            if (guildConfig == null)
            {
                Log.Error("No guild configuration found for the default guild!");
                return;
            }

            while (!token.IsCancellationRequested)
            {
                var tasks = new List<Task>();

                var guild = _client.GetGuild(guildConfig.Id);
                var scheduleChannels = GetScheduleOutputChannels().ToList();
                if (scheduleChannels.Any(c => c == null)) continue;

#if DEBUG
                Log.Information("Checking events.");
#endif

                foreach (var channel in scheduleChannels)
                {
                    tasks.Add(CheckRuns(guild, channel.Id, 120, async (host, embedMessage, embed) =>
                    {
                        var queue = await GetEventQueue(guildConfig, embedMessage, embed);
                        if (queue == null)
                        {
                            Log.Error("Reading event with null queue!");
                            return;
                        }

                        if (!embed.Footer.HasValue)
                        {
                            Log.Error("Reading event with null footer!");
                            return;
                        }
                        var eventId = ulong.Parse(embed.Footer.Value.Text);

                        var queueSlots = queue.GetEventSlots(eventId.ToString())
                            .Select(s => s.Id)
                            .Distinct()
                            .ToList();

                        Log.Information("Sending {SlotCount} notifications for event {EventId}.", queueSlots.Count, eventId);
                        var notificationTasks = new List<Task>();
                        notificationTasks.AddRange(queueSlots
                            .Select(userId => SendUserDM(_client.GetUser(userId), $"There are 2 hours until event `{eventId}`.\n" +
                                "Reply to this message with `~confirm` to confirm your spot or you will be removed 30 minutes before the event begins.")));
                        await Task.WhenAll(notificationTasks);
                    }, token));

                    tasks.Add(CheckRuns(guild, channel.Id, 60, async (host, embedMessage, embed) =>
                    {
                        var queue = await GetEventQueue(guildConfig, embedMessage, embed);
                        if (queue == null)
                        {
                            Log.Error("Reading event with null queue!");
                            return;
                        }

                        if (!embed.Footer.HasValue)
                        {
                            Log.Error("Reading event with null footer!");
                            return;
                        }
                        var eventId = ulong.Parse(embed.Footer.Value.Text);

                        var queueSlots = queue.GetEventSlots(eventId.ToString())
                            .Where(s => !s.Confirmed)
                            .Select(s => s.Id)
                            .Distinct()
                            .ToList();

                        Log.Information("Sending {SlotCount} second round notifications for event {EventId}.", queueSlots.Count, eventId);
                        var notificationTasks = new List<Task>();
                        notificationTasks.AddRange(queueSlots
                            .Select(userId => SendUserDM(_client.GetUser(userId), "There are now 30 minutes until this confirmation window closes. " +
                                "Please confirm your queue position by using the command `~confirm` in this DM.")));
                        await Task.WhenAll(notificationTasks);
                    }, token));

                    tasks.Add(CheckRuns(guild, channel.Id, 30, async (host, embedMessage, embed) =>
                    {
                        var queue = await GetEventQueue(guildConfig, embedMessage, embed);
                        if (queue == null)
                        {
                            Log.Error("Reading event with null queue!");
                            return;
                        }

                        if (!embed.Footer.HasValue)
                        {
                            Log.Error("Reading event with null footer!");
                            return;
                        }
                        var eventId = ulong.Parse(embed.Footer.Value.Text);

                        var queueSlots = queue.GetEventSlots(eventId.ToString())
                            .Where(s => !s.Confirmed)
                            .Select(s => s.Id)
                            .Distinct()
                            .ToList();

                        queue.DropUnconfirmed(eventId.ToString());

                        Log.Information("Sending {SlotCount} timeout notifications for event {EventId}.", queueSlots.Count, eventId);
                        var notificationTasks = new List<Task>();
                        notificationTasks.AddRange(queueSlots
                            .Select(userId => SendUserDM(_client.GetUser(userId), "The confirmation window has closed; you have been removed from the queue.\n" +
                                "You may reenter the queue using the event reactions or the queue channel, but you will be placed in the back of the queue.")));
                        await Task.WhenAll(notificationTasks);
                    }, token));
                }

                await Task.WhenAll(tasks);
                await Task.Delay(new TimeSpan(0, 5, 0), token);
            }
        }

        private static async Task CheckRuns(SocketGuild guild, ulong channelId, int minutesBefore, Func<SocketGuildUser, IMessage, IEmbed, Task> onMatch, CancellationToken token)
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
                    
                    if (timestamp.AddMinutes(60) < DateTimeOffset.Now)
                        continue;

                    // ReSharper disable once InvertIf
                    if (timestamp.AddMinutes(-minutesBefore) <= DateTimeOffset.Now && embed.Author.HasValue)
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

        private async Task<bool> DRSIsFreshProg(DiscordGuildConfiguration guildConfig, IMessage embedMessage, IEmbed embed)
        {
            if (!embed.Footer.HasValue) return false;
            var eventId = ulong.Parse(embed.Footer.Value.Text);

            var guild = _client.GetGuild(guildConfig.Id);
            var inputChannel = guild.GetTextChannel(GetScheduleInputChannel(guildConfig, embedMessage.Channel.Id));
            var eventMessage = await inputChannel.GetMessageAsync(eventId);

            var host = guild.GetUser(eventMessage.Author.Id);
            var discordRoles = DelubrumProgressionRoles.Roles.Keys
                .Select(rId => guild.GetRole(rId));
            var authorHasProgressionRole = discordRoles.Any(dr => host.HasRole(dr));
            var freshProg = !authorHasProgressionRole || eventMessage.Content.ToLowerInvariant().Contains("810201516291653643");
            return freshProg;
        }

        private async Task<FFXIVDiscordIntegratedQueue> GetEventQueue(DiscordGuildConfiguration guildConfig, IMessage embedMessage, IEmbed eventInfo)
        {
#if DEBUG
            return _queueService.GetOrCreateQueue(QueueInfo.LfgChannels[766712049316265985]);
#else
            var channelId = embedMessage.Channel.Id;
            if (channelId == guildConfig.DelubrumScheduleOutputChannel)
            {
                if (await DRSIsFreshProg(guildConfig, embedMessage, eventInfo))
                {
                    return _queueService.GetOrCreateQueue("lfg-drs-fresh-prog");
                }

                return _queueService.GetOrCreateQueue("lfg-delubrum-savage");
            }
            else if (channelId == guildConfig.DelubrumNormalScheduleOutputChannel)
            {
                return _queueService.GetOrCreateQueue("lfg-delubrum-normal");
            }
            else if (channelId == guildConfig.CastrumScheduleOutputChannel)
            {
                return _queueService.GetOrCreateQueue("lfg-castrum");
            }
            else
            {
                return null;
            }
#endif
        }

        private IEnumerable<ITextChannel> GetScheduleOutputChannels()
        {
            var guildConfig = _db.Guilds.FirstOrDefault(g => g.Id == SpecialGuilds.CrystalExploratoryMissions);
            if (guildConfig == null)
            {
                Log.Error("No guild configuration found for the default guild!");
                return Enumerable.Empty<ITextChannel>();
            }

            var guild = _client.GetGuild(guildConfig.Id);

            return new ulong[]
            {
#if DEBUG
                572084654069514241,
#else
                guildConfig.DelubrumScheduleOutputChannel,
                guildConfig.DelubrumNormalScheduleOutputChannel,
                guildConfig.CastrumScheduleOutputChannel,
#endif
            }.Select(c => guild?.GetTextChannel(c));
        }

        private static ulong GetScheduleInputChannel(DiscordGuildConfiguration guildConfig, ulong channelId)
        {
            if (channelId == guildConfig.CastrumScheduleOutputChannel)
                return guildConfig.CastrumScheduleInputChannel;
            else if (channelId == guildConfig.DelubrumNormalScheduleOutputChannel)
                return guildConfig.DelubrumNormalScheduleInputChannel;
            else if (channelId == guildConfig.DelubrumScheduleOutputChannel)
                return guildConfig.DelubrumScheduleInputChannel;
            else if (channelId == guildConfig.ScheduleOutputChannel)
                return guildConfig.ScheduleInputChannel;
            return 0;
        }

        private static async Task SendUserDM(IUser user, string message)
        {
#if DEBUG
            Log.Information("Sending direct message to user {User}.", user.ToString());
#endif

            try
            {
                await user.SendMessageAsync(message);
            }
            catch (HttpException e) when (e.DiscordCode == 50007)
            {
                Log.Warning("Can't send direct message to user {User}.", user.ToString());
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to DM user {User}!", user.ToString());
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