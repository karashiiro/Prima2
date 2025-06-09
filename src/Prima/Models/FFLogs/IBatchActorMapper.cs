using System.Collections.Generic;
using System.Threading.Tasks;
using Prima.Game.FFXIV;

namespace Prima.Models.FFLogs
{
    public interface IBatchActorMapper
    {
        Task<Dictionary<int, DiscordXIVUser?>> GetUsersFromActors(
            IEnumerable<LogInfo.ReportDataWrapper.ReportData.Report.Master.Actor> actors);
    }
}