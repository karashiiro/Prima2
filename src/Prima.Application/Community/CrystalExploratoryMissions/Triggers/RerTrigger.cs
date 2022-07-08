using Discord.WebSocket;

namespace Prima.Application.Community.CrystalExploratoryMissions.Triggers;

public class RerTrigger : CrystalExploratoryMissionsTrigger
{
    public override bool Condition(SocketMessage message)
    {
        return message.Content.Contains("649745887517605898");
    }

    public override async Task Execute(DiscordSocketClient client, SocketMessage message)
    {
        var guild = client.GetGuild(GetApplicableGuildId());
        var banish = await guild.GetEmoteAsync(846589900719259648);
        await message.AddReactionAsync(banish);
    }
}