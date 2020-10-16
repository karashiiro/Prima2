using System.Linq;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using Prima.Services;

namespace Prima.Stable.Modules
{
    [Name("Owner")]
    [RequireOwner]
    public class OwnerModule : ModuleBase<SocketCommandContext>
    {
        [Command("sendmessage")]
        public Task SudoMessage(ITextChannel channel, [Remainder] string message)
            => channel.SendMessageAsync(message);
    }
}
