using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Prima.Resources;
using Serilog;

namespace Prima.DiscordNet.Services
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
                if (_client.LoginState != LoginState.LoggedIn) continue;

                try
                {
                    var cem = _client.GetGuild(SpecialGuilds.CrystalExploratoryMissions);
                    if (cem == null)
                    {
                        await Task.Delay(new TimeSpan(0, 0, 5), token).ConfigureAwait(false);
                        continue;
                    }

                    Log.Information("Cleaning rosters...");

                    var rosterChannels = RosterChannels.Channels
                        .Values
                        .ToDictionary(id => id, id => cem.GetTextChannel(id));

                    foreach (var (id, rosterChannel) in rosterChannels)
                    {
                        if (rosterChannel == null)
                        {
                            Log.Information("Got null roster channel in KeepClean with ID: {ChannelId}!", id);
                            continue;
                        }

                        await CleanChannel(rosterChannel, new TimeSpan(72, 0, 0));
                    }

                    await Task.Delay(new TimeSpan(0, 5, 0), token).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Something bad happened in KeepClean");
                }
            }
        }

        private static async Task CleanChannel(IMessageChannel channel, TimeSpan limit)
        {
            await foreach (var page in channel.GetMessagesAsync())
            {
                foreach (var message in page)
                {
                    if (message.Id == 835621994753294456) continue;
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

                    await Task.Delay(1000);
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