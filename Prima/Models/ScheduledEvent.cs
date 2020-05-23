using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Prima.Resources;

namespace Prima.Models
{
    public class ScheduledEvent
    {
        [BsonId]
        private ObjectId _id { get; set; }

        public ulong GuildId { get; set; }

        public ulong MessageId { get; set; }

        public ulong EmbedMessageId { get; set; }

        public ulong LeaderId { get; set; }

        public long RunTime { get; set; }

        public string Description { get; set; }

        public RunDisplayType RunKind { get; set; }

        public IList<ulong> SubscribedUsers { get; set; }

        public bool Notified { get; set; }

        public bool Listed { get; set; }
    }
}
