using System.Threading.Tasks;
using Discord.Commands;
using Prima.Services;

namespace Prima.Stable.Modules
{
    [Name("Vote")]
    public class VoteModule : ModuleBase<SocketCommandContext>
    {
        public IDbService Db { get; set; }

        [Command("setvotehost")]
        [RequireOwner]
        public async Task SetVoteHost(ulong channelId, ulong messageId)
        {
            if (!await Db.AddVoteHost(messageId, Context.User.Id))
            {
                await ReplyAsync("That message is already registered as a vote host.");
                return;
            }

            var channel = Context.Guild.GetTextChannel(channelId);
            var message = await channel.GetMessageAsync(messageId);
            foreach (var (emote, _) in message.Reactions)
            {
                await foreach (var reaction in message.GetReactionUsersAsync(emote, 100))
                {
                    foreach (var user in reaction)
                    {
                        if (user.Id == message.Author.Id) continue;
                        await Db.AddVote(messageId, user.Id, emote.Name);
                        await message.RemoveReactionAsync(emote, user);
                    }
                }
            }

            await ReplyAsync("Message registered.");
        }
    }
}