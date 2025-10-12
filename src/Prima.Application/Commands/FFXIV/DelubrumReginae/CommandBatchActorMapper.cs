using Prima.Game.FFXIV;
using Prima.Models.FFLogs;
using Prima.Services;

namespace Prima.Application.Commands.FFXIV.DelubrumReginae;

public class CommandBatchActorMapper : IBatchActorMapper
{
    private readonly IDbService _db;

    public CommandBatchActorMapper(IDbService db)
    {
        _db = db;
    }

    public async Task<Dictionary<int, DiscordXIVUser?>> GetUsersFromActors(
        IEnumerable<LogInfo.ReportDataWrapper.ReportData.Report.Master.Actor> actors)
    {
        // Map to dict of actor ID to potential DB user
        var potentialUsers = actors.ToDictionary(a => a.Id, a => a)
            .Select(kvp => new
            {
                ActorId = kvp.Key,
                User = new PotentialDbUser(kvp.Value.Name, kvp.Value.Server),
            })
            .ToList();

        // Dedupe by world/name to minimize DB queries since the same user can show up as many actors
        var dedupedUsersForQuery = potentialUsers.Select(kvp => kvp.User)
            .DistinctBy(u => u.ToString())
            .Select(u => _db.GetUserByCharacterInfo(u.World, u.Name))
            .ToList();
        var dedupedUsersForQueryAwaited = await Task.WhenAll(dedupedUsersForQuery);

        // Reshape to dict of potential DB user to real DB user
        var dedupedUsersForQueryDict = dedupedUsersForQueryAwaited
            .Where(u => u != null)
            .ToDictionary(u => new PotentialDbUser(u.Name, u.World).ToString());

        // Build dict of actor ID to real DB user
        var users = potentialUsers
            .ToDictionary(
                kvp => kvp.ActorId,
                kvp => dedupedUsersForQueryDict.GetValueOrDefault(kvp.User.ToString()));
        return users;
    }
}