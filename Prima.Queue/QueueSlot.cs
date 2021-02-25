using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Prima.Queue
{
    public class QueueSlot
    {
        // Doing it this way gives us a drop-in reference replacement for ValueTuple.
        [JsonProperty("Item1")]
        public ulong Id { get; set; }

        [JsonProperty("Item2")]
        public DateTime QueueTime { get; set; }

        [JsonProperty("Item3")]
        public bool ExpirationNotified { get; set; }

        [JsonProperty("Item4")]
        private string eventId;

        [JsonIgnore]
        public string EventId
        {
            get => eventId ?? "";
            set => eventId = value;
        }

        [JsonProperty("Item5")]
        private IEnumerable<ulong> roleIds;

        [JsonIgnore]
        public IEnumerable<ulong> RoleIds
        {
            get => roleIds ?? new List<ulong>();
            set => roleIds = value;
        }

        [JsonProperty("Item6")]
        private IEnumerable<EventSlotState> eventIds;

        [JsonIgnore]
        public IEnumerable<EventSlotState> EventIds
        {
            get
            {
                if (eventIds != null)
                    return eventIds;
                eventIds = new List<EventSlotState>
                {
                    new EventSlotState(eventId),
                };
                return eventIds;
            }
            set => eventIds = value;
        }

        public QueueSlot(ulong id, string eventId = "", IEnumerable<ulong> roleIds = null, IEnumerable<EventSlotState> eventIds = null)
        {
            Id = id;
            QueueTime = DateTime.UtcNow;
            ExpirationNotified = false;
            EventId = eventId;
            RoleIds = roleIds ?? new ulong[] { };
            EventIds = eventIds ?? new[] { new EventSlotState(eventId) };
        }
    }
}