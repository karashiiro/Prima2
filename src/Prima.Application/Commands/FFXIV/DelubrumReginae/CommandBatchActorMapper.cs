using Discord;
using Discord.WebSocket;
using NetStone;
using Prima.Game.FFXIV;
using Prima.Models.FFLogs;
using Prima.Services;
using Serilog;

namespace Prima.Application.Commands.FFXIV.DelubrumReginae;

public class CommandBatchActorMapper : IBatchActorMapper
{
    private readonly IDbService _db;
    private readonly LodestoneClient _lodestone;
    private readonly SocketGuild _guild;

    public CommandBatchActorMapper(IDbService db, LodestoneClient lodestone, SocketGuild guild)
    {
        _db = db;
        _lodestone = lodestone;
        _guild = guild;
    }

    public async Task<Dictionary<int, DiscordXIVUser?>> GetUsersFromActors(
        IEnumerable<LogInfo.ReportDataWrapper.ReportData.Report.Master.Actor> actors)
    {
        var members = _guild.Users;
        var potentialUsers = actors.ToDictionary(a => a.Id, a => a)
            .Select(kvp => new KeyValuePair<int, PotentialDbUser>(kvp.Key, new PotentialDbUser(kvp.Value.Name,
                kvp.Value.Server, _db.Users.FirstOrDefault(u =>
                    string.Equals(u.Name, kvp.Value.Name, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(u.World, kvp.Value.Server, StringComparison.InvariantCultureIgnoreCase)))))
            .Select(async kvp =>
            {
                var (id, potentialUser) = kvp;
                if (potentialUser.User != null)
                {
                    // Already registered
                    return new KeyValuePair<int, DiscordXIVUser?>(id, potentialUser.User);
                }

                try
                {
                    // Attempt to register on the fly
                    await RegisterUser(members, potentialUser);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Failed to register user");
                }

                return new KeyValuePair<int, DiscordXIVUser?>(id, potentialUser.User);
            })
            .ToList();
        // We can't cleanly go from a KeyValuePair<int, Task<DiscordXIVUser>>
        // to a KeyValuePair<int, DiscordXIVUser>, so let's break it up into
        // multiple queries.
        var users = (await Task.WhenAll(potentialUsers))
            .Where(kvp => kvp.Value != null)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        return users;
    }

    private async Task RegisterUser(IEnumerable<IGuildUser> members, PotentialDbUser potentialUser)
    {
        var member = members.FirstOrDefault(m => m.Nickname == $"({potentialUser.World}) {potentialUser.Name}");
        if (member == null) return;

        try
        {
            var (userInfo, _) =
                await DiscordXIVUser.CreateFromLodestoneSearch(_lodestone, potentialUser.Name, potentialUser.World,
                    member.Id);
            await _db.AddUser(userInfo);
            potentialUser.User = userInfo;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to get user");
        }
    }
}