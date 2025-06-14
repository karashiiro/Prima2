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
        var potentialUsers = actors.ToDictionary(a => a.Id, a => a)
            .Select(async kvp => new
            {
                ActorId = kvp.Key,
                User = await _db.GetUserByCharacterInfo(kvp.Value.Server, kvp.Value.Name),
            })
            .ToList();
        var potentialUsersAwaited = await Task.WhenAll(potentialUsers);
        var users = potentialUsersAwaited
            .Where(kvp => kvp.User != null)
            .ToDictionary(kvp => kvp.ActorId, kvp => kvp.User);
        return users;
    }
}