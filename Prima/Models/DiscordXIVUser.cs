using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Prima.Models
{
    public class DiscordXIVUser
    {
        [BsonId]
        [BsonRequired]
        private ObjectId _id;

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public ulong DiscordId;

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public string LodestoneId;

        [BsonRequired]
        public string World;

        [BsonRequired]
        public string Name;

        [BsonRequired]
        public string Avatar;
    }
}
