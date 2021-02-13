using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Prima.Services;
using Serilog;

namespace Prima.Scheduler.Handlers
{
    public static class AnnounceReact
    {
        public static async Task HandlerAdd(DiscordSocketClient client, DbService db, Cacheable<IUserMessage, ulong> cachedMessage, ulong userId)
        {
            var eventId = await GetEventId(cachedMessage);
            if (eventId == null) return;
            if (await db.AddEventReaction(eventId.Value, userId))
            {
                var user = client.GetUser(userId);
                Log.Information("User {User} has signed up for notifications for event {EventId}.", user.ToString(), eventId);
            }
        }

        public static async Task HandlerRemove(DiscordSocketClient client, DbService db, Cacheable<IUserMessage, ulong> cachedMessage, ulong userId)
        {
            var eventId = await GetEventId(cachedMessage);
            if (eventId == null) return;
            if (await db.RemoveEventReaction(eventId.Value, userId))
            {
                var user = client.GetUser(userId);
                Log.Information("User {User} has been removed from notifications for event {EventId}.", user.ToString(), eventId);
            }
        }

        private static async Task<ulong?> GetEventId(Cacheable<IUserMessage, ulong> cachedMessage)
        {
            var message = await cachedMessage.GetOrDownloadAsync();
            var embed = message.Embeds.FirstOrDefault(e => e.Type == EmbedType.Rich);
            if (embed?.Footer == null) return null;

            if (!ulong.TryParse(embed.Footer?.Text, out var eventId)) return null;

            return eventId;
        }
    }
}