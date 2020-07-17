using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Prima.Stable.Modules
{
    public class OwnerModule : ModuleBase<SocketCommandContext>
    {
        [Command("sendmessage")]
        [RequireOwner]
        public Task SudoMessage(ITextChannel channel, [Remainder] string message)
            => channel.SendMessageAsync(message);
    }
}
