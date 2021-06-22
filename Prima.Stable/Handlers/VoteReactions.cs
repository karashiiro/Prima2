using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Prima.DiscordNet.Services;

namespace Prima.Stable.Handlers
{
    public static class VoteReactions
    {
        public static async Task HandlerAdd(DiscordSocketClient client, IDbService db, Cacheable<IUserMessage, ulong> cachedMessage, SocketReaction reaction)
        {
            var message = await cachedMessage.GetOrDownloadAsync();

            var messageId = message.Id;
            var userId = reaction.UserId;
            var reactionName = reaction.Emote.Name;

            if (message.Author.Id == userId) return;

            var voteHost = await db.VoteHosts
                .FirstOrDefaultAsync(vh => vh.MessageId == messageId);
            if (voteHost == null) return;

            await db.RemoveVote(messageId, userId);
            await db.AddVote(messageId, userId, reactionName);

            var user = client.GetUser(userId);
            await user.SendMessageAsync($"You have voted for option `{reactionName}`.");

            await message.RemoveReactionAsync(reaction.Emote, userId);
        }
    }
}