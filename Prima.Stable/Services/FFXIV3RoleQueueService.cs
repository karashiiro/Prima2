using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Prima.Services
{
    public class FFXIV3RoleQueueService
    {
        private static string QueuePath => Environment.OSVersion.Platform == PlatformID.Win32NT
            ? "queues.json" // Only use Windows for testing.
            : Path.Combine(Environment.GetEnvironmentVariable("HOME"), "queues.json");

        public IDictionary<string, FFXIV3RoleQueue> Queues { get; private set; }

        public FFXIV3RoleQueueService()
        {
            if (!File.Exists(QueuePath))
                Queues = new Dictionary<string, FFXIV3RoleQueue>();
            else Load();
        }

        public FFXIV3RoleQueue GetOrCreateQueue(string name)
        {
            if (Queues.ContainsKey(name)) return Queues[name];
            Queues.Add(name, new FFXIV3RoleQueue());
            return Queues[name];
        }

        public void Save()
        {
            File.WriteAllText(QueuePath, JsonConvert.SerializeObject(Queues));
        }

        public void Load()
        {
            Queues = JsonConvert.DeserializeObject<IDictionary<string, FFXIV3RoleQueue>>(File.ReadAllText(QueuePath));
        }
    }
}
