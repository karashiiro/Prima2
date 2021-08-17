using System.Linq;
using Discord;
using Discord.Commands;
using Prima.Services;
using System.Threading.Tasks;

namespace Prima.Stable.Modules
{
    [Name("Owner")]
    [RequireOwner]
    public class OwnerModule : ModuleBase<SocketCommandContext>
    {
        public IDbService Db { get; set; }

        [Command("sendmessage")]
        public Task SudoMessage(ITextChannel channel, [Remainder] string message)
            => channel.SendMessageAsync(message);

        [Command("clearbrokenusers")]
        public async Task ClearBrokenUsers()
        {
            await Db.RemoveBrokenUsers();
            await ReplyAsync("Done!");
        }
    }
}
