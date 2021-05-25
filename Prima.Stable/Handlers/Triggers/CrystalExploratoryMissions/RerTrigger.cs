using System.Threading.Tasks;
using Discord.WebSocket;

namespace Prima.Stable.Handlers.Triggers.CrystalExploratoryMissions
{
    public class RerTrigger : CrystalExploratoryMissionsTrigger
    {
        public override bool Condition(SocketMessage message)
        {
            return message.Content.Contains("153329450350673920");
        }

        public override async Task Execute(DiscordSocketClient client, SocketMessage message)
        {
            var guild = client.GetGuild(GetApplicableGuildId());
            var banish = await guild.GetEmoteAsync(846589900719259648);
            await message.AddReactionAsync(banish);
        }
    }
}