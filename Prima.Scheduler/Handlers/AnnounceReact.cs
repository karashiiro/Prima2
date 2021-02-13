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
        public static async Task HandlerAdd(DiscordSocketClient client, DbService db, Cacheable<IUserMessage, ulong> cachedMessage, SocketReaction reaction)
        {
            var userId = reaction.UserId;
            if (client.CurrentUser.Id == userId || reaction.Emote.Name != "📳") return;

            var eventId = await GetEventId(cachedMessage);
            if (eventId == null) return;
            if (await db.AddEventReaction(eventId.Value, userId))
            {
                var user = client.GetUser(userId);
                await user.SendMessageAsync($"You have signed up for notifications for run {eventId}!");
                Log.Information("User {User} has signed up for notifications for event {EventId}.", user.ToString(), eventId);
            }
            else if (await db.RemoveEventReaction(eventId.Value, userId))
            {
                var user = client.GetUser(userId);
                await user.SendMessageAsync($"You have been removed from the notifications list for event {eventId}.");
                Log.Information("User {User} has been removed from notifications for event {EventId}.", user.ToString(), eventId);
            }

            var message = await cachedMessage.GetOrDownloadAsync();
            try
            {
                await message.RemoveReactionAsync(new Emoji("📳"), userId);
            }
            catch { /* ignored */ }
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