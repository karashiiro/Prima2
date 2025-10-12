using System;
using System.Collections.Generic;
using System.Linq;
using Prima.Resources;
using Serilog;

namespace Prima.Game.FFXIV.FFLogs.Rules
{
    /// <summary>
    /// Parsing rules specific to Delubrum Reginae (Savage).
    /// </summary>
    public class DelubrumReginaeSavageRules : ILogParsingRules
    {
        public ulong FinalClearRoleId => DelubrumProgressionRoles.ClearedDelubrumSavage;

        public string GetProgressionRoleName(string encounterName)
        {
            var roleName = encounterName switch
            {
                "The Queen's Guard" => "Queen's Guard",
                _ => encounterName,
            };

            roleName += " Progression";

            // Validate that this role name exists in our mapping
            return DelubrumProgressionRoles.Roles.Any(kvp => kvp.Value == roleName) ? roleName : null;
        }

        public ulong GetKillRoleId(string progressionRoleName)
        {
            try
            {
                return DelubrumProgressionRoles.GetKillRole(progressionRoleName);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to get kill role for progression role: {ProgressionRole}", progressionRoleName);
                return 0;
            }
        }

        public IEnumerable<ulong> GetContingentRoleIds(ulong killRoleId)
        {
            return killRoleId == DelubrumProgressionRoles.ClearedDelubrumSavage
                ? DelubrumProgressionRoles.Roles.Keys
                : DelubrumProgressionRoles.GetContingentRoles(killRoleId);
        }

        public bool ShouldProcessEncounter(int? difficulty)
        {
            // DRS Savage encounters have difficulty != 100
            // Normal mode encounters have difficulty == 100
            return difficulty != 100;
        }
    }
}