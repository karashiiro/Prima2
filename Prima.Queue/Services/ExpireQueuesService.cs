using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Prima.Models;
using Prima.Resources;
using Prima.Services;
using Serilog;

namespace Prima.Queue.Services
{
    public class ExpireQueuesService : IDisposable
    {
        private readonly DiscordSocketClient _client;
        private readonly FFXIV3RoleQueueService _queueService;
        private readonly IDbService _db;

        private Thread _loopThread;
        private volatile bool _active;

        public ExpireQueuesService(DiscordSocketClient client, FFXIV3RoleQueueService queueService, IDbService db)
        {
            _client = client;
            _queueService = queueService;
            _db = db;
            Start();
        }

        private void Start()
        {
            if (_active) return;
            _active = true;
            _loopThread = ExpireLoop();
            _loopThread.Start();
        }

        private void Stop()
        {
            if (!_active) return;
            _active = false;
            _loopThread.Join();
            _loopThread = null;
        }

        private Thread ExpireLoop()
        {
            return new(() =>
            {
                while (_active)
                {
                    if (_client.LoginState != LoginState.LoggedIn) continue;
                    
                    foreach (var guild in _client.Guilds)
                    {
                        var eventIds = (GetEvents(guild, int.MaxValue).ConfigureAwait(false).GetAwaiter().GetResult())
                            .Select(@event => @event.Item2.Footer?.Text)
                            .Where(id => id != null)
                            .ToList();
                        if (!eventIds.Any()) continue;

                        var queueEventIds = new List<string>();

                        var queues = QueueInfo.LfgChannels
                            .Select(kvp => kvp.Value)
                            .Select(_queueService.GetOrCreateQueue)
                            .ToList();
                        foreach (var queue in queues)
                        {
                            queueEventIds.AddRange(queue.GetEvents());
                        }

                        var inactiveEventIds = queueEventIds
                            .Except(eventIds)
                            .Distinct()
                            .ToList();
                        foreach (var queue in queues)
                        {
                            foreach (var eventId in inactiveEventIds)
                            {
                                queue.ExpireEvent(eventId);
                                Log.Information("Cleared queue for event {EventId}", eventId);
                            }
                        }

                        _queueService.Save();

                        Log.Information($"Events cleared in guild {guild.Name}:{inactiveEventIds.Aggregate("", (agg, next) => agg + $"\n  {next}")}");
                    }

                    Thread.Sleep(86400000);
                }
            });
        }

        private async Task<IEnumerable<(IMessage, IEmbed)>> GetEvents(SocketGuild guild, int inHours)
        {
            var guildConfig = _db.Guilds.FirstOrDefault(conf => conf.Id == guild.Id);
            if (guildConfig == null) return new List<(IMessage, IEmbed)>();

            var channels = GetOutputChannels(guild);

            var events = new List<(IMessage, IEmbed)>();
            foreach (var channel in channels)
            {
                if (channel == null) continue;
                await foreach (var page in channel.GetMessagesAsync())
                {
                    // ReSharper disable once LoopCanBeConvertedToQuery
                    foreach (var message in page)
                    {
                        var embed = message.Embeds.FirstOrDefault(e => e.Type == EmbedType.Rich);
                        if (embed?.Footer == null) continue;

                        if (embed.Timestamp == null) continue;
                        var timestamp = embed.Timestamp.Value;
                        if ((DateTimeOffset.UtcNow - timestamp).TotalHours > inHours) continue;

                        events.Add((message, embed));
                    }
                }
            }

            return events;
        }

        private IEnumerable<ITextChannel> GetOutputChannels(SocketGuild guild)
        {
            var guildConfig = _db.Guilds.FirstOrDefault(g => g.Id == (guild?.Id ?? 0));
            if (guildConfig == null) return new List<ITextChannel>();

            var scheduleOutputChannels = typeof(DiscordGuildConfiguration).GetFields()
                .Where(f => RegexSearches.ScheduleOutputFieldNameRegex.IsMatch(f.Name))
                .Select(f => (ulong?)f.GetValue(guildConfig))
                .Select(cId => guild.GetTextChannel(cId ?? 0));

            return scheduleOutputChannels;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}