using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Prima.Models
{
    public class GlobalConfiguration
    {
        [BsonId]
        [BsonRequired]
        private ObjectId _id;

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public ulong BotMaster = 128581209109430272;

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public char Prefix = '~';

        [BsonRequired]
        public string TempDir = "temp";
    }
}
