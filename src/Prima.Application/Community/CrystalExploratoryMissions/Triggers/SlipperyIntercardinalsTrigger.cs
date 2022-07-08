using Discord;
using Discord.WebSocket;
using Prima.Application.Triggers.Attributes;

namespace Prima.Application.Community.CrystalExploratoryMissions.Triggers;

public class IntercardinalsTrigger : CrystalExploratoryMissionsTrigger
{
    public override bool Condition(SocketMessage message)
    {
        return HasWord(message.Content, "intercardinals")
               || HasWord(message.Content, "intercards")
               || message.Content.Contains("383805961216983061");
    }

    [ShouldRunFirst]
    public override async Task Execute(DiscordSocketClient client, SocketMessage message)
    {
        var emotes = new[] { new Emoji("↖️"), new Emoji("↙️"), new Emoji("↘️"), new Emoji("↗️") };
        foreach (var emote in emotes)
        {
            await message.AddReactionAsync(emote);
        }
    }
}