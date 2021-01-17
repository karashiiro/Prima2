﻿using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Prima.Models
{
    public class ChannelDescription
    {
        [BsonId]
        [BsonRequired]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "<Pending>")]
#pragma warning disable 169
        private ObjectId _id;
#pragma warning restore 169

        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public ulong ChannelId { get; set; }

        [BsonRequired]
        public string Description { get; set; }
    }
}
