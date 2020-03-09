using Discord;
using Discord.Commands;
using Prima.Services;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Prima.Modules
{
    /// <summary>
    /// Includes commands that all bot instances should be able to execute.
    /// </summary>
    [Name("Common")]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public class CommonModule : ModuleBase<SocketCommandContext>
    {
        public DiagnosticService Diagnostics { get; set; }

        [Command("ping", RunMode = RunMode.Async)]
        public async Task PingAsync()
        {
            await ReplyAsync($"`{Process.GetCurrentProcess().ProcessName} online, heartbeat {Diagnostics.GetLatency()}ms`");
        }
    }
}