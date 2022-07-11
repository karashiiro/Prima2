using System.Diagnostics;
using Discord.Commands;

namespace Prima.Application.Commands.Core;

[Name("Diagnostics")]
[RequireOwner]
public class DiagnosticsCommands : ModuleBase<SocketCommandContext>
{
    private readonly CommandService _commands;

    public DiagnosticsCommands(CommandService commands)
    {
        _commands = commands;
    }

    [Command("ping", RunMode = RunMode.Async)]
    public async Task PingAsync()
    {
        await ReplyAsync($"`{Process.GetCurrentProcess().ProcessName} online, heartbeat {Context.Client.Latency}ms`");
    }

    [Command("modules")]
    public Task Modules()
    {
        return ReplyAsync(_commands.Modules
            .Select(module => module.Name)
            .Aggregate("```\n", (acc, next) => acc + next + "\n") + "```");
    }
}