using System.Threading.Tasks;
using Prima.Models.FFLogs;

namespace Prima.Game.FFXIV.FFLogs
{
    public interface ILogParserService
    {
        /// <summary>
        /// Reads a fight log to compute progression role updates.
        /// </summary>
        /// <param name="logLink">The FFLogs log link.</param>
        /// <param name="actorMapper">A helper to fetch users based on FFLogs actors.</param>
        /// <returns>The parsing result, which includes the roles to add/remove.</returns>
        Task<LogParsingResult> ReadLog(string logLink, IBatchActorMapper actorMapper);
    }
}