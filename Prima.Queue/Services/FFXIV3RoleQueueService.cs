using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Serilog;

namespace Prima.Queue.Services
{
    public class FFXIV3RoleQueueService
    {
        private static string QueuePath => Environment.OSVersion.Platform == PlatformID.Win32NT
            ? "queues.json" // Only use Windows for testing.
            : Path.Combine(Environment.GetEnvironmentVariable("HOME"), "queues.json");

        private IDictionary<string, FFXIV3RoleQueue> Queues { get; set; }

        private readonly DiscordSocketClient _client;

        public FFXIV3RoleQueueService(DiscordSocketClient client)
        {
            if (!File.Exists(QueuePath))
                Queues = new Dictionary<string, FFXIV3RoleQueue>();
            else Load();

            _client = client;

            _ = TimeoutLoop();
        }

        public FFXIV3RoleQueue GetOrCreateQueue(string name)
        {
            lock (Queues)
            {
                if (Queues.ContainsKey(name)) return Queues[name];
                Queues.Add(name, new FFXIV3RoleQueue());
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
            Queues = JsonConvert.DeserializeObject<IDictionary<string, FFXIV3RoleQueue>>(File.ReadAllText(QueuePath));
        }

        private async Task TimeoutLoop()
        {
            const int second = 1000;

            while (true)
            {
                await Task.Delay(300 * second);

                await AlertTimeouts(Queues["learning-and-frag-farm"]?.Timeout(10800, 0), "learning-and-frag-farm", 3);
                await AlertTimeouts(Queues["av-and-ozma-prog"]?.Timeout(10800, 0), "av-and-ozma-prog", 3);
                await AlertTimeouts(Queues["clears-and-farming"]?.Timeout(10800, 0), "clears-and-farming", 3);
                await AlertTimeouts(Queues["lfg-castrum"]?.Timeout(3600, 900), "lfg-castrum", 1);

                Save();
            }
        }

        private async Task AlertTimeouts((IEnumerable<ulong> uids, IEnumerable<ulong> almostUids)? sets, string queueName, int hours)
        {
            if (sets == null) return;

            var (uids, almostUids) = sets.Value;

            foreach (var uid in uids)
            {
                var user = _client.GetUser(uid);
                Log.Information("Timed out {User} from queue {QueueName}.", user.ToString(), queueName);
                await user.SendMessageAsync($"You have been in the queue `#{queueName}` for {hours} hours and have been timed-out.\n" +
                                            "This is a measure in place to avoid leads having to pull numerous AFK users before your run.\n" +
                                            "Please rejoin the queue if you are still active.");
            }
            
            foreach (var uid in almostUids)
            {
                var user = _client.GetUser(uid);
                Log.Information("Warned {User} of imminent timeout from queue {QueueName}.", user.ToString(), queueName);
                await user.SendMessageAsync($"You have been in the queue `#{queueName}` for almost {hours} hours.\n" +
                                            "To avoid being removed for inactivity, please use the command `~refresh`.");
            }
        }
    }
}
