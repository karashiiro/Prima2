using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Prima.Models
{
    public class DiscordGuildConfiguration
    {
        [BsonId]
        [BsonRequired]
        [BsonRepresentation(BsonType.ObjectId)]
        public ulong Id;

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public ulong ScheduleInputChannel = 0;

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public ulong ScheduleOutputChannel = 0;

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public ulong StatusChannel = 0;

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public ulong DeletedMessageChannel = 0;

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public ulong DeletedCommandChannel = 0;

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public ulong ReportChannel = 0;

        [BsonRequired]
        public IDictionary<string, string> Roles = new Dictionary<string, string>();

        [BsonRequired]
        public IList<string> TextBlacklist = new List<string>();

        [BsonRequired]
        public char Prefix = '\u0000';

        [BsonRequired]
        public int MinimumLevel = 0;

        [BsonRequired]
        public string BACalendarId = string.Empty;

        public DiscordGuildConfiguration(ulong guildId) => Id = guildId;
    }
}