using Discord.Commands;
using Prima.Services;

namespace Prima.Application.Commands.Random;

[Name("Voting")]
public class VotingCommands : ModuleBase<SocketCommandContext>
{
    private readonly IDbService _db;

    public VotingCommands(IDbService db)
    {
        _db = db;
    }

    [Command("setvotehost")]
    [RequireOwner]
    public async Task SetVoteHost(ulong channelId, ulong messageId)
    {
        if (!await _db.AddVoteHost(messageId, Context.User.Id))
        {
            await ReplyAsync("That message is already registered as a vote host.");
            return;
        }

        var channel = Context.Guild.GetTextChannel(channelId);
        var message = await channel.GetMessageAsync(messageId);
        foreach (var (emote, _) in message.Reactions)
        {
            await foreach (var reaction in message.GetReactionUsersAsync(emote, 100))
            {
                foreach (var user in reaction)
                {
                    if (user.Id == message.Author.Id) continue;
                    await _db.AddVote(messageId, user.Id, emote.Name);
                    await message.RemoveReactionAsync(emote, user);
                }
            }
        }

        await ReplyAsync("Message registered.");
    }
}