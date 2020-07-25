using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Prima.Models
{
    public class ChannelDescription
    {
        [BsonId]
        [BsonRequired]
        private ObjectId _id;

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public ulong ChannelId { get; set; }

        [BsonRequired]
        public string Description { get; set; }
    }
}
