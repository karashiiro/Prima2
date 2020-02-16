using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Yisoft.Crontab;
using DiscordId = System.UInt64;
using Member = System.Collections.Generic.KeyValuePair<ulong, long>;
using MemberEntries = System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<ulong, long>>;

namespace Prima.Services
{
    public class QueueService
    {
        private readonly ConfigurationService _config;
        private readonly DiscordSocketClient _client;

        private readonly Dictionary<string, Queue> _queues;

        private Task _runningTask;

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
        public QueueService(ConfigurationService config, DiscordSocketClient client)
        {
            _config = config;
            _client = client;
            _queues = new Dictionary<string, Queue>();

            string[] queueFiles = Directory.GetFiles(_config.QueueDir);
            foreach (string queueFile in queueFiles)
            {
                Log.Information("Loading file {File}", queueFile);
                string fileData = File.ReadAllText(_config.QueueDir);
                var saveData = (SaveData)JsonConvert.DeserializeObject(fileData);
                _queues.Add(queueFile, new Queue(saveData.TimeLimit, saveData.Members));
            }

            _runningTask = SaveQueues();
        }

        /// <summary>
        /// Adds a queue.
        /// </summary>
        /// <param name="queueName">The full file name of the queue to add.</param>
        /// <param name="timeLimit">The queue timeout.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void CreateQueue(string queueName, long timeLimit)
        {
            Log.Information("Adding queue {File}", queueName);
            _queues.Add(queueName, new Queue(timeLimit, new MemberEntries()));
        }

        /// <summary>
        /// Deletes a queue.
        /// </summary>
        /// <param name="queueName">The full file name of the queue to delete.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="PathTooLongException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public void DeleteQueue(string queueName)
        {
            Log.Information("Removing queue {File}", queueName);
            _queues.Remove(queueName);
            File.Delete(Path.Combine(_config.QueueDir, queueName));
        }

        public void Enqueue(string queueName, DiscordId userId) => _queues[queueName].Enqueue(userId);
        public void Dequeue(string queueName) => _queues[queueName].Dequeue();
        public void Dequeue(string queueName, DiscordId userId) => _queues[queueName].Dequeue(userId);

        public long GetTimeLimit(string queueName) => _queues[queueName].TimeLimit;

        public bool IsFaulted() => _runningTask.IsFaulted;

        [Cron("*/5 * * * * *")]
        public async Task WarnTimeouts()
        {
            foreach (KeyValuePair<string, Queue> q in _queues)
            {
                DiscordId[] userIds = q.Value.GetTimeoutCandidatesOnce();
                long warnTime = q.Value.TimeLimit * (1 - (1 / 12));
                long hours = (long)Math.Floor((double)(q.Value.TimeLimit / 60000.0));
                long minutes = (q.Value.TimeLimit - (long)Math.Floor((double)(q.Value.TimeLimit / 60000) * 60000)) / 60000;
                foreach (DiscordId userId in userIds)
                {
                    IUser user = _client.GetUser(userId);
                    IDMChannel userChannel = await user.GetOrCreateDMChannelAsync();
                    await userChannel.SendMessageAsync(
                        $"You have been in this queue for {hours} hours and {minutes} minutes:" +
                        q.Key.Substring(0, q.Key.LastIndexOf(".")) +
                        $"Please send `~refreshqueue` within the next {warnTime * (1 / 12.0)} minutes to avoid being kicked under our {hours}:{minutes} time limit." +
                        $"This command will renew your queue times and allow you to stay queued for an additional {hours}:{minutes}."
                        );
                }
            }
        }

        [Cron("*/5 * * * * *")]
        public async Task Timeout()
        {
            foreach (KeyValuePair<string, Queue> q in _queues)
            {
                DiscordId[] userIds = q.Value.GetTimeoutCandidates();
                long hours = (long)Math.Floor((double)(q.Value.TimeLimit / 60000.0));
                long minutes = (q.Value.TimeLimit - (long)Math.Floor((double)(q.Value.TimeLimit / 60000) * 60000)) / 60000;
                foreach (DiscordId userId in userIds)
                {
                    IUser user = _client.GetUser(userId);
                    IDMChannel userChannel = await user.GetOrCreateDMChannelAsync();
                    await userChannel.SendMessageAsync(
                        $"You have been in this queue for more than {hours}:{minutes}, and have been removed:" +
                        q.Key.Substring(0, q.Key.LastIndexOf(".")) +
                        "Please requeue if you are still active."
                        );
                }
                Log.Information("Timed {UserCount} members out of {Name}.", userIds.Length, q.Key);
            }
        }

        private async Task SaveQueues()
        {
            while (true)
            {
                await Task.Delay(60000);
                foreach (KeyValuePair<string, Queue> q in _queues)
                {
                    await File.WriteAllTextAsync(Path.Combine(_config.QueueDir, q.Key), JsonConvert.SerializeObject(new SaveData
                    {
                        TimeLimit = q.Value.TimeLimit,
                        Members = q.Value.Members,
                    }));
                }
            }
        }

        private class Queue
        {
            public long TimeLimit;
            public MemberEntries Members;

            public Queue(long timeLimit, MemberEntries members)
            {
                TimeLimit = timeLimit;
                Members = members;
            }

            public void Enqueue(DiscordId userId)
            {
                Members.Add(new Member(userId, DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
            }

            public DiscordId Dequeue()
            {
                Members.Sort((member1, member2) => member1.Value.CompareTo(member2.Value));
                return Members.First().Key;
            }

            public DiscordId Dequeue(DiscordId userId)
            {
                Members.RemoveAll((member) => member.Key == userId);
                return userId;
            }

            public DiscordId[] GetTimeoutCandidates()
            {
                return Members
                    .Where((member) => DateTimeOffset.UtcNow.ToUnixTimeSeconds() - member.Value >= TimeLimit * (1 / 12))
                    .Select((member) => member.Key)
                    .ToArray();
            }

            public DiscordId[] GetTimeoutCandidatesOnce()
            {
                return Members
                    .Where((member) =>
                        {
                            long timeDiff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - member.Value;
                            if (timeDiff >= TimeLimit * (1 / 12) - 300000) return false;
                            if (timeDiff >= TimeLimit * (1 / 12)) return true;
                            return false;
                        })
                    .Select((member) => member.Key)
                    .ToArray();
            }

            public DiscordId[] Timeout()
            {
                var members = Members
                    .Where((member) => DateTimeOffset.UtcNow.ToUnixTimeSeconds() - member.Value >= TimeLimit)
                    .Select((member) => member.Key)
                    .ToArray();
                Members.RemoveAll((member => DateTimeOffset.UtcNow.ToUnixTimeSeconds() - member.Value < TimeLimit));
                return members;
            }
        }

        private struct SaveData
        {
            public long TimeLimit;
            public MemberEntries Members;
        }
    }
}
