using MongoDB.Driver;

namespace Prima.Data;

public class RoleReactionsDb : IRoleReactionsDb
{
    private const string DbName = "PrimaDb";
    private const string CollectionName = "RoleReactions";

    private readonly IMongoClient _client;

    public RoleReactionsDb(IMongoClient client)
    {
        _client = client;
    }

    public async Task<IList<RoleReactionInfo>> GetRoleReactions(ulong guildId)
    {
        var db = _client.GetDatabase(DbName);
        var coll = db.GetCollection<RoleReactionInfo>(CollectionName);
        var filter = Builders<RoleReactionInfo>.Filter.Eq(o => o.GuildId, guildId.ToString());
        return await coll.Find(filter).ToListAsync();
    }

    public async Task<RoleReactionInfo?> GetRoleReaction(ulong guildId, ulong channelId, ulong emoteId)
    {
        var db = _client.GetDatabase(DbName);
        var coll = db.GetCollection<RoleReactionInfo>(CollectionName);
        var filter = Builders<RoleReactionInfo>.Filter.Eq(o => o.GuildId, guildId.ToString());
        return await coll.Find(filter).FirstOrDefaultAsync();
    }

    public async Task CreateRoleReaction(RoleReactionInfo rrInfo)
    {
        EnsureParameters(rrInfo);
        var db = _client.GetDatabase(DbName);
        var coll = db.GetCollection<RoleReactionInfo>(CollectionName);
        await coll.InsertOneAsync(rrInfo);
    }

    public async Task<bool> RemoveRoleReaction(RoleReactionInfo rrInfo)
    {
        EnsureParameters(rrInfo);
        var db = _client.GetDatabase(DbName);
        var coll = db.GetCollection<RoleReactionInfo>(CollectionName);
        var filter1 = Builders<RoleReactionInfo>.Filter.Eq(o => o.GuildId, rrInfo.GuildId);
        var filter2 = Builders<RoleReactionInfo>.Filter.Eq(o => o.ChannelId, rrInfo.ChannelId);
        var filter3 = Builders<RoleReactionInfo>.Filter.Eq(o => o.RoleId, rrInfo.RoleId);
        var filter = Builders<RoleReactionInfo>.Filter.And(filter1, filter2, filter3);
        var result = await coll.DeleteManyAsync(filter);
        return result.DeletedCount >= 1;
    }

    public async Task<RoleReactionInfo?> UpdateRoleReaction(RoleReactionInfo rrInfo)
    {
        EnsureParameters(rrInfo);
        var db = _client.GetDatabase(DbName);
        var coll = db.GetCollection<RoleReactionInfo>(CollectionName);

        var filter1 = Builders<RoleReactionInfo>.Filter.Eq(o => o.GuildId, rrInfo.GuildId);
        var filter2 = Builders<RoleReactionInfo>.Filter.Eq(o => o.ChannelId, rrInfo.ChannelId);
        var filter3 = Builders<RoleReactionInfo>.Filter.Eq(o => o.RoleId, rrInfo.RoleId);
        var filter = Builders<RoleReactionInfo>.Filter.And(filter1, filter2, filter3);

        var update1 = Builders<RoleReactionInfo>.Update.Set(o => o.Eureka, rrInfo.Eureka);
        var update2 = Builders<RoleReactionInfo>.Update.Set(o => o.Bozja, rrInfo.Bozja);
        var update3 = Builders<RoleReactionInfo>.Update.Set(o => o.EmojiId, rrInfo.EmojiId);
        var update = Builders<RoleReactionInfo>.Update.Combine(update1, update2, update3);

        var result = await coll.FindOneAndUpdateAsync(filter, update);
        return result;
    }

    private static void EnsureParameters(RoleReactionInfo rrInfo)
    {
        if (rrInfo.GuildId == null)
        {
            throw new ArgumentException("Guild ID must not be null", nameof(rrInfo));
        }

        if (rrInfo.ChannelId == null)
        {
            throw new ArgumentException("Channel ID must not be null", nameof(rrInfo));
        }

        if (rrInfo.RoleId == null)
        {
            throw new ArgumentException("Role ID must not be null", nameof(rrInfo));
        }
    }
}