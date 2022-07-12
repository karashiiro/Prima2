using Discord;
using Discord.Commands;
using Prima.Services;

namespace Prima.Application.Commands.Core;

[Name("Admin 2")]
[RequireOwner]
public class AdminCommands2 : ModuleBase<SocketCommandContext>
{
    private readonly IDbService _db;

    public AdminCommands2(IDbService db)
    {
        _db = db;
    }

    [Command("sendmessage")]
    public async Task SudoMessage(ITextChannel channel, [Remainder] string message)
    {
        await channel.SendMessageAsync(message);
        await ReplyAsync("Sent!");
    }

    [Command("clearbrokenusers")]
    public async Task ClearBrokenUsers()
    {
        await _db.RemoveBrokenUsers();
        await ReplyAsync("Done!");
    }
}