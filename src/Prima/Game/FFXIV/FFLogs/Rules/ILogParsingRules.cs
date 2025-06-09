using System.Collections.Generic;

namespace Prima.Game.FFXIV.FFLogs.Rules
{
    /// <summary>
    /// Parsing rules specific to different content types.
    /// </summary>
    public interface ILogParsingRules
    {
        /// <summary>
        /// Maps encounter names to progression role names.
        /// </summary>
        /// <param name="encounterName">The encounter name from the log</param>
        /// <returns>The progression role name, or null if not applicable</returns>
        string GetProgressionRoleName(string encounterName);

        /// <summary>
        /// Gets the kill role information for a progression role.
        /// </summary>
        /// <param name="progressionRoleName">The progression role name</param>
        /// <returns>The kill role ID, or 0 if not found</returns>
        ulong GetKillRoleId(string progressionRoleName);

        /// <summary>
        /// Gets all contingent role IDs that should be added when someone gets a kill role.
        /// </summary>
        /// <param name="killRoleId">The kill role ID</param>
        /// <returns>List of role IDs that should also be assigned</returns>
        IEnumerable<ulong> GetContingentRoleIds(ulong killRoleId);

        /// <summary>
        /// Determines if an encounter should be processed based on difficulty.
        /// </summary>
        /// <param name="difficulty">The difficulty value from the log</param>
        /// <returns>True if the encounter should be processed</returns>
        bool ShouldProcessEncounter(int? difficulty);

        /// <summary>
        /// Gets the final clear role ID (e.g., "DRS Cleared").
        /// </summary>
        ulong FinalClearRoleId { get; }
    }
}