using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Prima.Services;
using Serilog;
using System.Threading.Tasks;

namespace Prima.Scheduler.Handlers
{
    public static class AnnounceReact
    {
        public static async Task HandlerAdd(DiscordClient client, IDbService db, MessageReactionAddEventArgs reactionEvent)
        {
            var guild = reactionEvent.Message.Channel.Guild;
            if (guild == null) return;

            var message = reactionEvent.Message;
            var user = await guild.GetMemberAsync(reactionEvent.User.Id);
            var userDmChannel = await user.CreateDmChannelAsync();

            if (client.CurrentUser.Id == user.Id || reactionEvent.Emoji.Name != "📳") return;

            var eventId = AnnounceUtil.GetEventId(message);
            if (eventId == null) return;
            if (await db.AddEventReaction(eventId.Value, user.Id))
            {
                await userDmChannel.SendMessageAsync($"You have signed up for notifications for event {eventId}!");
                Log.Information("User {User} has signed up for notifications for event {EventId}.", user.ToString(), eventId);
            }
            else if (await db.RemoveEventReaction(eventId.Value, user.Id))
            {
                await userDmChannel.SendMessageAsync($"You have been removed from the notifications list for event {eventId}.");
                Log.Information("User {User} has been removed from notifications for event {EventId}.", user.ToString(), eventId);
            }

            try
            {
                await message.DeleteReactionAsync(DiscordEmoji.FromUnicode("📳"), user);
            }
            catch { /* ignored */ }
        }
    }
}