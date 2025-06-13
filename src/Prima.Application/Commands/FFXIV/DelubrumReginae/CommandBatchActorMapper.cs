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

    public Task<Dictionary<int, DiscordXIVUser?>> GetUsersFromActors(
        IEnumerable<LogInfo.ReportDataWrapper.ReportData.Report.Master.Actor> actors)
    {
        var potentialUsers = actors.ToDictionary(a => a.Id, a => a)
            .Select(kvp => new KeyValuePair<int, PotentialDbUser>(kvp.Key, new PotentialDbUser(kvp.Value.Name,
                kvp.Value.Server, _db.Users.FirstOrDefault(u =>
                    string.Equals(u.Name, kvp.Value.Name, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(u.World, kvp.Value.Server, StringComparison.InvariantCultureIgnoreCase)))))
            .Select(kvp =>
            {
                var (id, potentialUser) = kvp;
                return new KeyValuePair<int, DiscordXIVUser?>(id, potentialUser.User);
            })
            .ToList();
        var users = potentialUsers
            .Where(kvp => kvp.Value != null)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        return Task.FromResult(users);
    }
}