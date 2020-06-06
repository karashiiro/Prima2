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

        [BsonRepresentation(BsonType.String)]
        public ulong GuildId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public ulong MessageId2 { get; set; }

        [BsonRepresentation(BsonType.String)]
        public ulong EmbedMessageId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public ulong LeaderId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public long RunTime { get; set; }

        [BsonRepresentation(BsonType.String)]
        public string Description { get; set; }

        public RunDisplayType RunKind { get; set; }

        public IList<string> SubscribedUsers { get; set; }

        public bool Notified { get; set; }

        public bool Listed { get; set; }
    }
}
