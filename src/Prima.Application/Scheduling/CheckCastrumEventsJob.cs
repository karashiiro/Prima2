using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Prima.DiscordNet;
using Prima.Resources;
using Prima.Services;
using Quartz;

namespace Prima.Application.Scheduling;

public class CheckCastrumEventsJob : CheckEventChannelJob, IJob
{
    public CheckCastrumEventsJob(ILogger<CheckCastrumEventsJob> logger, DiscordSocketClient client, IDbService db) : base(logger, client, db)
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

        return Task.FromResult(guildConfig.CastrumScheduleOutputChannel);
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
        
        var success = await AssignHostRole(guild);
        if (!success) return;

        await NotifyHost("The Castrum run you scheduled is set to begin in 30 minutes!");
        await NotifyMembers($"The Castrum run you reacted to (hosted by {HostUser?.Nickname ?? HostUser?.Username}) is beginning in 30 minutes!");
    }
    
    private async Task<bool> AssignHostRole(SocketGuild guild)
    {
        var currentHost = guild.GetRole(RunHostData.RoleId);
        var runPinner = guild.GetRole(RunHostData.PinnerRoleId);

        Logger.LogInformation("Assigning roles...");
        if (HostUser == null || HostUser.HasRole(currentHost)) return false;

        try
        {
            await HostUser.AddRolesAsync(new[] { currentHost, runPinner });
            await Db.AddTimedRole(currentHost.Id, guild.Id, HostUser.Id, DateTime.UtcNow.AddHours(4.5));
            await Db.AddTimedRole(runPinner.Id, guild.Id, HostUser.Id, DateTime.UtcNow.AddHours(4.5));
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to add host role to {User}!", currentHost?.ToString() ?? "null");
            return false;
        }
    }
}