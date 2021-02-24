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
        public async Task SetVoteHost(ulong messageId)
        {
            if (!await Db.AddVoteHost(messageId, Context.User.Id))
            {
                await ReplyAsync("That message is already registered as a vote host.");
                return;
            }
            await ReplyAsync("Message registered.");
        }
    }
}