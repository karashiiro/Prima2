using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Prima.Models
{
    public class CachedMessage
    {
        [BsonId]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "<Pending>")]
#pragma warning disable 169
        private ObjectId _id;
#pragma warning restore 169

        public ulong AuthorId { get; set; }

        public ulong ChannelId { get; set; }

        public ulong MessageId { get; set; }

        public string Content { get; set; }

        public long UnixMs { get; set; }
    }
}
