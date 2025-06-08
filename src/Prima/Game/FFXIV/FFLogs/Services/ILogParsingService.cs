using System.Threading.Tasks;
using Prima.Game.FFXIV.FFLogs.Models;

namespace Prima.Game.FFXIV.FFLogs.Services
{
    /// <summary>
    /// Service for parsing FFLogs and determining role assignments
    /// </summary>
    public interface ILogParsingService
    {
        /// <summary>
        /// Parses a log and determines what role assignments should be made
        /// </summary>
        /// <param name="request">The parsing request configuration</param>
        /// <returns>The result containing role assignments</returns>
        Task<LogParsingResult> ParseLogAsync(LogParsingRequest request);
    }
}
