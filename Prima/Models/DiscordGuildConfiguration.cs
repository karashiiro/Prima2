using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Prima.Models
{
    public class DiscordGuildConfiguration
    {
        [BsonId]
        [BsonRequired]
        private ObjectId _id;

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
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
        public ulong WelcomeChannel = 0;

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public ulong ReportChannel = 0;

        [BsonRequired]
        public IDictionary<string, string> Roles = new Dictionary<string, string>();

        [BsonRequired]
        public IList<string> TextBlacklist = new List<string>();

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public char Prefix = ' ';

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public int MinimumLevel = 0;

        [BsonRequired]
        public string BACalendarId = string.Empty;

        [BsonRequired]
        public string BACalendarLink = string.Empty;

        public DiscordGuildConfiguration(ulong guildId) => Id = guildId;
    }
}