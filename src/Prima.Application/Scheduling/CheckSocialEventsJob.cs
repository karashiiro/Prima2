using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Prima.DiscordNet;
using Prima.Resources;
using Prima.Services;

namespace Prima.Application.Scheduling;

public class CheckSocialEventsJob : CheckEventChannelJob
{
    public CheckSocialEventsJob(ILogger<CheckSocialEventsJob> logger, DiscordSocketClient client, IDbService db) : base(logger, client, db)
    {
    }

    protected override Task<ulong> GetGuildId()
    {
        return Task.FromResult(SpecialGuilds.CrystalExploratoryMissions);
    }

    protected override Task<ulong> GetChannelId()
    {
        var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == SpecialGuilds.CrystalExploratoryMissions);
        if (guildConfig == null)
        {
            throw new InvalidOperationException("No guild configuration found for the default guild!");
        }

        return Task.FromResult(guildConfig.SocialScheduleOutputChannel);
    }

    protected override async Task OnMatch()
    {
        var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == SpecialGuilds.CrystalExploratoryMissions);
        if (guildConfig == null)
        {
            Logger.LogError("No guild configuration found for the default guild!");
            return;
        }
        
        var guild = Client.GetGuild(SpecialGuilds.CrystalExploratoryMissions);
        
        var success = await AssignSocialHostRole(guild);
        if (!success) return;

        await NotifyHost("The event you scheduled is set to begin in 30 minutes!");
        await NotifyMembers($"The event you reacted to (hosted by {HostUser?.Nickname ?? HostUser?.Username}) is beginning in 30 minutes!");
    }
    
    private async Task<bool> AssignSocialHostRole(SocketGuild guild)
    {
        var socialHost = guild.GetRole(RunHostData.SocialHostRoleId);

        Logger.LogInformation("Assigning roles...");
        if (HostUser == null || HostUser.HasRole(socialHost)) return false;

        try
        {
            await Db.AddTimedRole(socialHost.Id, guild.Id, HostUser.Id, DateTime.UtcNow.AddHours(4.5));
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to add host role to {User}!", socialHost?.ToString() ?? "null");
            return false;
        }
    }
}