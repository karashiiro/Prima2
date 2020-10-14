using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Prima.Models
{
    public class DiscordGuildConfiguration
    {
        [BsonId]
        [BsonRequired]
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "<Pending>")]
        [SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
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

        [BsonRepresentation(BsonType.String)]
        public ulong CastrumScheduleInputChannel = 0;

        [BsonRepresentation(BsonType.String)]
        public ulong CastrumScheduleOutputChannel = 0;

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
        public IDictionary<string, string> RoleEmotes = new Dictionary<string, string>();

        [BsonRequired]
        public IList<string> TextBlacklist = new List<string>();

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public char Prefix = ' ';

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public int MinimumLevel = 0;

        [Obsolete]
        public string BACalendarId = string.Empty;

        [Obsolete]
        public string BACalendarLink = string.Empty;

        public string BASpreadsheetId = string.Empty;

        public string BASpreadsheetLink = string.Empty;

        public string CastrumSpreadsheetId = string.Empty;

        public string CastrumSpreadsheetLink = string.Empty;

        public DiscordGuildConfiguration(ulong guildId) => Id = guildId;
    }
}