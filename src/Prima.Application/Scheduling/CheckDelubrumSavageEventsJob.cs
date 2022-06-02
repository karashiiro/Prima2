using Discord;
using Discord.WebSocket;
using Prima.Application.Logging;
using Prima.DiscordNet;
using Prima.Resources;
using Prima.Services;

namespace Prima.Application.Scheduling;

public class CheckDelubrumSavageEventsJob : CheckEventChannelJob
{
    public CheckDelubrumSavageEventsJob(IAppLogger logger, DiscordSocketClient client, IDbService db) : base(logger, client, db)
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
            Logger.Error("No guild configuration found for the default guild!");
            return;
        }
        
        var guild = Client.GetGuild(SpecialGuilds.CrystalExploratoryMissions);
        
        var success = await AssignHostRole(guild);
        if (!success) return;

        await AssignExecutorRole(guild);
        await NotifyMembers($"The Delubrum Reginae (Savage) run you reacted to (hosted by {HostUser?.Nickname ?? HostUser?.Username}) is beginning in 30 minutes!");
        await NotifyHost(
            "You have been given the Delubrum Host role for 4 1/2 hours!\n" +
            "You can now use the command `~setroler @User` to give them access to the progression " +
            "role commands `~addprogrole @User Role Name` and `~removeprogrole @User Role Name`!\n" +
            "You can also modify multiple users at once by using `~addprogrole @User1 @User2 Role Name`.\n\n" +
            "Available roles:\n" +
            "▫️ Trinity Seeker Progression\n" +
            "▫️ Queen's Guard Progression\n" +
            "▫️ Trinity Avowed Progression\n" +
            "▫️ The Queen Progression");
    }
    
    private async Task<bool> AssignHostRole(SocketGuild guild)
    {
        var currentHost = guild.GetRole(RunHostData.RoleId);
        var runPinner = guild.GetRole(RunHostData.PinnerRoleId);

        Logger.Info("Assigning roles...");
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
            Logger.Error(e, "Failed to add host role to {User}!", currentHost?.ToString() ?? "null");
            return false;
        }
    }
    
    private async Task AssignExecutorRole(SocketGuild guild)
    {
        var executor = guild.GetRole(DelubrumProgressionRoles.Executor);
        
        if (HostUser == null || HostUser.HasRole(executor)) return;
        
        await HostUser.AddRoleAsync(executor);
        await Db.AddTimedRole(executor.Id, guild.Id, HostUser.Id, DateTime.UtcNow.AddHours(4.5));
    }
}