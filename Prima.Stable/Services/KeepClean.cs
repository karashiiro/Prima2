using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Prima.Resources;
using Serilog;

namespace Prima.Stable.Services
{
    public class KeepClean : IDisposable
    {
        private readonly DiscordSocketClient _client;

        private readonly CancellationTokenSource _tokenSource;

        public KeepClean(DiscordSocketClient client)
        {
            _client = client;
            _tokenSource = new CancellationTokenSource();
        }

        public void Initialize()
        {
            _ = CheckLoop(_tokenSource.Token);
        }

        private async Task CheckLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var cem = _client.GetGuild(SpecialGuilds.CrystalExploratoryMissions);
                if (cem == null) continue;

                var rosterChannels = RosterChannels.Channels
                    .Values
                    .Select(id => cem.GetTextChannel(id));

                foreach (var rosterChannel in rosterChannels)
                {
                    if (rosterChannel == null)
                    {
                        Log.Information("Got null roster channel in KeepClean!");
                        continue;
                    }

                    await CleanChannel(rosterChannel, new TimeSpan(72, 0, 0));
                }
            }
        }

        private static async Task CleanChannel(IMessageChannel channel, TimeSpan limit)
        {
            await foreach (var page in channel.GetMessagesAsync())
            {
                foreach (var message in page)
                {
                    if (DateTimeOffset.UtcNow - message.Timestamp <= limit) continue;
                    try
                    {
                        await message.DeleteAsync();
                        Log.Information("Message deleted in channel {ChannelId}", channel.Id);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Failed to delete message from channel {ChannelId} in KeepClean!", channel.Id);
                    }
                }
            }
        }

        public void Dispose()
        {
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
        }
    }
}