using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Prima.Models;
using Prima.Services;

namespace Prima.Stable.Services
{
    // Caches the last week's worth of messages manually, since the built-in cache is apparently awful.
    public class MessageCacheService
    {
        private readonly DbService _db;

        public MessageCacheService(DbService db)
        {
            _db = db;
        }

        public async Task CacheMessage(IMessage message)
        {
            var cmessage = new CachedMessage
            {
                AuthorId = message.Author.Id,
                ChannelId = message.Channel.Id,
                MessageId = message.Id,
                Content = message.Content,
                UnixMs = message.Timestamp.ToUnixTimeMilliseconds(),
            };
            await _db.CacheMessage(cmessage);

            var oldMessages = _db.CachedMessages.Where(m => DateTimeOffset.FromUnixTimeMilliseconds(m.UnixMs) >= DateTimeOffset.Now.AddDays(-7));
            foreach (var om in oldMessages)
            {
                await _db.DeleteMessage(om.MessageId);
            }
        }
    }
}
