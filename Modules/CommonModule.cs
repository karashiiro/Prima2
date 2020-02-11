using Discord.Commands;
using Prima.Services;
using System.Threading.Tasks;

namespace Prima.Modules
{
    /// <summary>
    /// Includes commands that all bot instances should be able to execute, regardless of <see cref="Preset"/>.
    /// </summary>
    [Name("Common")]
    [RequireOwner]
    public class CommonModule : ModuleBase<SocketCommandContext>
    {
        public CommandHandlingService CommandHandler { get; set; }
        public ConfigurationService Config { get; set; }
        public DiagnosticService Diagnostics { get; set; }

        // Responds with the configuration and the estimated latency of the bot.
        [Command("ping")]
        public async Task PingAsync()
        {
            await ReplyAsync($"`Prima {Config.CurrentPreset} online, heartbeat {Diagnostics.GetLatency()}ms`");
        }
        
        // Reloads the command assembly.
        [Command("reloadcommands")]
        public async Task ReloadCommandsAsync()
        {
            await CommandHandler.ReinitalizeAsync();
            await ReplyAsync(Properties.Resources.CommandAssemblyReloadSuccess);
        }
    }
}
