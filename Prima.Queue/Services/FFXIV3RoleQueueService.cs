using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using Prima.Resources;
using Serilog;

namespace Prima.Queue.Services
{
    public class FFXIV3RoleQueueService
    {
        private static string QueuePath => Environment.OSVersion.Platform == PlatformID.Win32NT
            ? "queues.json" // Only use Windows for testing.
            : Path.Combine(Environment.GetEnvironmentVariable("HOME"), "queues.json");

        private IDictionary<string, FFXIVDiscordIntegratedQueue> Queues { get; set; }

        private readonly DiscordSocketClient _client;

        public FFXIV3RoleQueueService(DiscordSocketClient client)
        {
            if (!File.Exists(QueuePath))
                Queues = new Dictionary<string, FFXIVDiscordIntegratedQueue>();
            else Load();

            _client = client;
            
            _ = Task.Run(TimeoutLoop);
        }

        public FFXIVDiscordIntegratedQueue GetOrCreateQueue(string name)
        {
            lock (Queues)
            {
                if (Queues.ContainsKey(name)) return Queues[name];
                Queues.Add(name, new FFXIVDiscordIntegratedQueue());
                return Queues[name];
            }
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(QueuePath, JsonConvert.SerializeObject(Queues));
            }
            catch
            {
                File.WriteAllText(QueuePath, JsonConvert.SerializeObject(Queues));
            }
        }

        private void Load()
        {
            Queues = JsonConvert.DeserializeObject<IDictionary<string, FFXIVDiscordIntegratedQueue>>(File.ReadAllText(QueuePath));
        }

        private async Task TimeoutLoop()
        {
            const int second = 1000;

            while (true)
            {
                await Task.Delay(300 * second);

                try
                {
                    await AlertTimeouts(Queues["learning-and-frag-farm"]?.Timeout(QueueInfo.BAQueueTimeout, 0 * Time.Second), "learning-and-frag-farm", 4);
                    await AlertTimeouts(Queues["av-and-ozma-prog"]?.Timeout(QueueInfo.BAQueueTimeout, 0 * Time.Second), "av-and-ozma-prog", 4);
                    await AlertTimeouts(Queues["clears-and-farming"]?.Timeout(QueueInfo.BAQueueTimeout, 0 * Time.Second), "clears-and-farming", 4);
                    await AlertTimeouts(Queues["lfg-castrum"]?.Timeout(QueueInfo.CastrumQueueTimeout, 0 * Time.Second), "lfg-castrum", 4);
                    await AlertTimeouts(Queues["lfg-delubrum-savage"]?.Timeout(QueueInfo.DelubrumQueueTimeout, 0 * Time.Second), "lfg-delubrum-savage", 4);
                    await AlertTimeouts(Queues["lfg-drs-fresh-prog"]?.Timeout(QueueInfo.DelubrumQueueTimeout, 0 * Time.Second), "lfg-drs-fresh-prog", 4);
                    await AlertTimeouts(Queues["lfg-delubrum-normal"]?.Timeout(QueueInfo.DelubrumQueueTimeout, 0 * Time.Second), "lfg-delubrum-normal", 4);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error in user timeout loop!");
                }

                Save();
            }
        }

        private async Task AlertTimeouts((IEnumerable<ulong> uids, IEnumerable<ulong> almostUids)? sets, string queueName, int hours)
        {
            if (sets == null) return;

            var (uids, almostUids) = sets.Value;

            async Task SendToQueueChannel(ulong uid)
            {
                var channelId = QueueInfo.FlipDictionary()[queueName][0];
                var channel = _client.GetChannel(channelId) as SocketTextChannel;
                await channel.SendMessageAsync(
                    $"<@{uid}>, you have been in the queue `#{queueName}` for {hours} hours and have been timed-out.\n" +
                    "This is a measure in place to avoid leads having to pull numerous AFK users before your run.\n" +
                    "Please rejoin the queue if you are still active.\n\n" +
                    "(Failed to DM; hit fallback,)");
            }

            foreach (var uid in uids)
            {
                var user = (IUser)_client.GetUser(uid) ?? await _client.Rest.GetUserAsync(uid);
                if (user == null)
                {
                    Log.Warning("User {User} could not be fetched, alerting in queue channel.", uid.ToString());
                    await SendToQueueChannel(uid);
                    continue;
                }

                Log.Information("Timed out {User} from queue {QueueName}.", user.ToString(), queueName);
                try
                {
                    await user.SendMessageAsync(
                        $"You have been in the queue `#{queueName}` for {hours} hours and have been timed-out.\n" +
                        "This is a measure in place to avoid leads having to pull numerous AFK users before your run.\n" +
                        "Please rejoin the queue if you are still active.");
                }
                catch (HttpException e) when (e.DiscordCode == 50007)
                {
                    Log.Warning("User {User} has disabled guild DMs.", user.ToString());
                    await SendToQueueChannel(uid);
                }
                catch (HttpException e)
                {
                    Log.Warning(e, "Messaging user {User} failed.", user.ToString());
                    await SendToQueueChannel(uid);
                }
            }
            
            foreach (var uid in almostUids ?? Enumerable.Empty<ulong>())
            {
                var user = (IUser)_client.GetUser(uid) ?? await _client.Rest.GetUserAsync(uid);
                try
                {
                    await user.SendMessageAsync(
                        $"You have been in the queue `#{queueName}` for almost {hours} hours.\n" +
                        "To avoid being removed for inactivity, please use the command `~refresh`.");
                    Log.Information("Warned {User} of imminent timeout from queue {QueueName}.", user.ToString(),
                        queueName);
                }
                catch (HttpException e) when (e.DiscordCode == 50007)
                {
                    Log.Warning("User {User} has disabled guild DMs.", user.ToString());
                }
                catch (HttpException e)
                {
                    Log.Warning(e, "Messaging user {User} failed.", user.ToString());
                }
            }
        }
    }
}
