using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Prima.Data;

public class RoleReactionInfo
{
    [BsonId]
    [BsonRequired]
    [BsonElement("_id")]
    public ObjectId Id { get; set; }

    [BsonElement("guild_id")] public string? GuildId { get; set; }

    [BsonElement("channel_id")] public string? ChannelId { get; set; }

    [BsonElement("emoji_id")] public string? EmojiId { get; set; }

    [BsonElement("role_id")] public string? RoleId { get; set; }

    [BsonElement("eureka")] public bool? Eureka { get; set; }

    [BsonElement("bozja")] public bool? Bozja { get; set; }
}