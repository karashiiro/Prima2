using Discord.WebSocket;
using System.Threading.Tasks;
using Discord;

namespace Prima.Stable.Handlers.Triggers.CrystalExploratoryMissions
{
    public class SlipperyTrigger : CrystalExploratoryMissionsTrigger
    {
        public override bool Condition(SocketMessage message)
        {
            return message.Content.Contains("383805961216983061");
        }

        public override async Task Execute(DiscordSocketClient client, SocketMessage message)
        {
            var guild = client.GetGuild(GetApplicableGuildId());
            var sadge = await guild.GetEmoteAsync(845161446547521566);
            await Task.WhenAll(message.AddReactionAsync(sadge), message.AddReactionAsync(new Emoji("🦶")));
        }
    }
}