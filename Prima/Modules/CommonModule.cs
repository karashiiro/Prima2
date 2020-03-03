using Discord.Commands;
using Prima.Models;
using Prima.Services;
using System;
using System.Threading.Tasks;

namespace Prima.Modules
{
    /// <summary>
    /// Includes commands that all bot instances should be able to execute.
    /// </summary>
    [Name("Common")]
    [RequireOwner]
    public class CommonModule : ModuleBase<SocketCommandContext>
    {
        public CommandHandlingService CommandHandler { get; set; }
        public DbService Db { get; set; }
        public DiagnosticService Diagnostics { get; set; }

        [Command("ping", RunMode = RunMode.Async)]
        public async Task PingAsync()
        {
            await ReplyAsync($"`{Environment.CurrentDirectory} online, heartbeat {Diagnostics.GetLatency()}ms`");
        }
        
        [Command("configureguild", RunMode = RunMode.Async)]
        public async Task ReloadCommandsAsync()
        {
            await Db.AddGuild(new DiscordGuildConfiguration(Context.Guild.Id));
            await ReplyAsync();
        }
    }
}