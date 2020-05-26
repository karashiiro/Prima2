using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Prima.Models
{
    public class CachedMessage
    {
        [BsonId]
        private ObjectId _id;

        public ulong AuthorId { get; set; }

        public ulong ChannelId { get; set; }

        public ulong MessageId { get; set; }

        public string Content { get; set; }

        public long UnixMs { get; set; }
    }
}
