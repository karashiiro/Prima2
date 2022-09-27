using Discord.WebSocket;

namespace Prima.Application.Triggers;

public class TableUnflipTrigger : BaseTrigger
{
    public override bool Condition(SocketMessage message)
    {
        return message.Content == "(╯°□°）╯︵ ┻━┻";
    }

    public override Task Execute(DiscordSocketClient client, SocketMessage message)
    {
        return message.Channel.SendMessageAsync("┬─┬ ノ( ゜-゜ノ)");
    }
}