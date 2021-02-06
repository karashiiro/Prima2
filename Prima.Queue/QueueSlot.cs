using System;
using Newtonsoft.Json;

namespace Prima.Queue
{
    public class QueueSlot
    {
        // Doing it this way should give us a drop-in reference replacement for ValueTuple.
        [JsonProperty("Item1")]
        public ulong Id { get; set; }

        [JsonProperty("Item2")]
        public DateTime QueueTime { get; set; }

        [JsonProperty("Item3")]
        public bool ExpirationNotified { get; set; }

        public QueueSlot(ulong id)
        {
            Id = id;
            QueueTime = DateTime.UtcNow;
            ExpirationNotified = false;
        }

        public void Deconstruct(out ulong id, out DateTime queueTime, out bool expirationNotified)
        {
            id = Id;
            queueTime = QueueTime;
            expirationNotified = ExpirationNotified;
        }
    }
}