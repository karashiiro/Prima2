using System.Threading.Tasks;
using Discord.WebSocket;

namespace Prima.Stable.Handlers.Triggers
{
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
}