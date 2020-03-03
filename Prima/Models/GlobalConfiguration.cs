using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Prima.Models
{
    public class GlobalConfiguration
    {
        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public ulong BotMaster;

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public char Prefix;

        [BsonRequired]
        public string TempDir;
    }
}
