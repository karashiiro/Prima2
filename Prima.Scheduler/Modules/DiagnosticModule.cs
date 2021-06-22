using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;

namespace Prima.Scheduler.Modules
{
    public class DiagnosticModule : BaseCommandModule
    {
        [Command("ping")]
        [RequireOwner]
        public Task Ping(CommandContext ctx)
        {
            return ctx.RespondAsync($"`Prima.Scheduler online, heartbeat {ctx.Client.Ping}ms");
        }
    }
}