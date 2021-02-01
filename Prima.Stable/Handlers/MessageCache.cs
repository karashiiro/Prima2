using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Prima.Models;
using Prima.Services;

namespace Prima.Stable.Handlers
{
    public static class MessageCache
    {
        // Caches the last week's worth of messages manually, since the built-in cache is apparently awful.
        public static async Task Handler(DbService db, IMessage message)
        {
            var cmessage = new CachedMessage
            {
                AuthorId = message.Author.Id,
                ChannelId = message.Channel.Id,
                MessageId = message.Id,
                Content = message.Content,
                UnixMs = message.Timestamp.ToUnixTimeMilliseconds(),
            };
            await db.CacheMessage(cmessage);

            var oldMessages = db.CachedMessages
                .Where(m => DateTimeOffset.FromUnixTimeMilliseconds(m.UnixMs) >= DateTimeOffset.Now.AddDays(-7));

            foreach (var om in oldMessages)
            {
                await db.DeleteMessage(om.MessageId);
            }
        }
    }
}