using System.Collections.Generic;

namespace Prima.Game.FFXIV.FFLogs.Models
{
    /// <summary>
    /// Configuration for parsing a log and determining role assignments
    /// </summary>
    public class LogParsingRequest
    {
        public string LogUrl { get; set; }
        public ILogParsingRules Rules { get; set; }
    }

    /// <summary>
    /// Defines the rules for parsing encounters and mapping them to roles
    /// </summary>
    public interface ILogParsingRules
    {
        /// <summary>
        /// Maps encounter names to progression role names
        /// </summary>
        /// <param name="encounterName">The encounter name from the log</param>
        /// <returns>The corresponding progression role name, or null if not applicable</returns>
        string GetProgressionRoleName(string encounterName);

        /// <summary>
        /// Gets the clear role name for a given progression role
        /// </summary>
        /// <param name="progressionRoleName">The progression role name</param>
        /// <returns>The clear role name, or null if not applicable</returns>
        string GetClearRoleName(string progressionRoleName);

        /// <summary>
        /// Gets all contingent roles that should be added when someone gets a progression role
        /// </summary>
        /// <param name="progressionRoleName">The progression role name</param>
        /// <returns>List of role names that should also be assigned</returns>
        IEnumerable<string> GetContingentRoles(string progressionRoleName);

        /// <summary>
        /// Determines if an encounter should be processed based on difficulty
        /// </summary>
        /// <param name="difficulty">The difficulty value from the log</param>
        /// <returns>True if the encounter should be processed</returns>
        bool ShouldProcessEncounter(int? difficulty);

        /// <summary>
        /// Gets the final clear role name (e.g., "DRS Cleared")
        /// </summary>
        string FinalClearRoleName { get; }
    }
}
