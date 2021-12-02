using System.Threading.Tasks;
using Discord.WebSocket;

namespace Prima.Stable.Handlers.Triggers.CrystalExploratoryMissions
{
    public class ScholarTrigger : CrystalExploratoryMissionsTrigger
    {
        public override bool Condition(SocketMessage message)
        {
            return HasWord(message.Content, "sch") || HasWord(message.Content, "scholar");
        }

        public override async Task Execute(DiscordSocketClient client, SocketMessage message)
        {
            var guild = client.GetGuild(GetApplicableGuildId());
            var emote = await guild.GetEmoteAsync(573531927613800459); // SCH emote
            await message.AddReactionAsync(emote);
        }
    }
}