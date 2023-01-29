using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Prima.DiscordNet;
using Prima.Resources;
using Prima.Services;

namespace Prima.Application.Scheduling;

public class CheckDelubrumSavageEventsJob : CheckEventChannelJob
{
    public CheckDelubrumSavageEventsJob(ILogger<CheckDelubrumSavageEventsJob> logger, DiscordSocketClient client,
        IDbService db) : base(logger, client, db)
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

        return Task.FromResult(guildConfig.DelubrumScheduleOutputChannel);
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

        var success = await AssignHostPreparingRole(guild);
        if (!success) return;

        await NotifyHost("The Delubrum Reginae (Savage) run you scheduled is set to begin in 30 minutes!");
        await NotifyMembers(
            $"The Delubrum Reginae (Savage) run you reacted to (hosted by {HostUser?.Nickname ?? HostUser?.Username}) is beginning in 30 minutes!");
    }

    protected override async Task OnMatch2()
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

        await AssignExecutorRole(guild);
        await NotifyHost(
            "You have been given the Delubrum Host role for 5 hours!\n" +
            "You can now use the command `~setroler @User` to give them access to the progression " +
            "role commands `~addprogrole @User Role Name` and `~removeprogrole @User Role Name`!\n" +
            "You can also modify multiple users at once by using `~addprogrole @User1 @User2 Role Name`.\n\n" +
            "Available roles:\n" +
            "▫️ Trinity Seeker Progression\n" +
            "▫️ Queen's Guard Progression\n" +
            "▫️ Trinity Avowed Progression\n" +
            "▫️ The Queen Progression");
    }

    private async Task<bool> AssignHostPreparingRole(SocketGuild guild)
    {
        var preparing = guild.GetRole(RunHostData.PreparingForEventRoleId);

        Logger.LogInformation("Assigning preparing role...");
        if (HostUser == null || HostUser.HasRole(preparing)) return false;

        try
        {
            await HostUser.AddRoleAsync(preparing);
            await Db.AddTimedRole(preparing.Id, guild.Id, HostUser.Id, DateTime.UtcNow.AddHours(1));
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to add preparing role to {User}!", HostUser?.ToString() ?? "null");
            return false;
        }
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
            await Db.AddTimedRole(currentHost.Id, guild.Id, HostUser.Id, DateTime.UtcNow.AddHours(5));
            await Db.AddTimedRole(runPinner.Id, guild.Id, HostUser.Id, DateTime.UtcNow.AddHours(5));
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to add host role to {User}!", HostUser?.ToString() ?? "null");
            return false;
        }
    }

    private async Task AssignExecutorRole(SocketGuild guild)
    {
        var executor = guild.GetRole(DelubrumProgressionRoles.Executor);

        if (HostUser == null || HostUser.HasRole(executor)) return;

        await HostUser.AddRoleAsync(executor);
        await Db.AddTimedRole(executor.Id, guild.Id, HostUser.Id, DateTime.UtcNow.AddHours(5));
    }
}