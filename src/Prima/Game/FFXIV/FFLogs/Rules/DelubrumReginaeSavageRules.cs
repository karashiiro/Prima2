using System.Collections.Generic;
using System.Linq;
using Prima.Game.FFXIV.FFLogs.Models;
using Prima.Resources;

namespace Prima.Game.FFXIV.FFLogs.Rules
{
    /// <summary>
    /// Parsing rules specific to Delubrum Reginae (Savage)
    /// </summary>
    public class DelubrumReginaeSavageRules : ILogParsingRules
    {
        public string FinalClearRoleName => "DRS Cleared";

        public string GetProgressionRoleName(string encounterName)
        {
            return encounterName switch
            {
                "Trinity Seeker" => "Trinity Seeker Progression",
                "The Queen's Guard" => "Queen's Guard Progression",
                "Queen's Guard" => "Queen's Guard Progression",
                "Trinity Avowed" => "Trinity Avowed Progression",
                "The Queen" => "The Queen Progression",
                _ => null
            };
        }

        public string GetClearRoleName(string progressionRoleName)
        {
            // For DRS, we use the existing DelubrumProgressionRoles mapping
            var roleId = DelubrumProgressionRoles.Roles
                .FirstOrDefault(kvp => kvp.Value == progressionRoleName).Key;
            
            if (roleId == 0) return null;

            try
            {
                var killRoleId = DelubrumProgressionRoles.GetKillRole(progressionRoleName);
                if (killRoleId == DelubrumProgressionRoles.ClearedDelubrumSavage)
                {
                    return "DRS Cleared";
                }
                return DelubrumProgressionRoles.Roles.TryGetValue(killRoleId, out var killRoleName) 
                    ? killRoleName 
                    : null;
            }
            catch (System.NotImplementedException)
            {
                return null;
            }
        }

        public IEnumerable<string> GetContingentRoles(string progressionRoleName)
        {
            var roleId = DelubrumProgressionRoles.Roles
                .FirstOrDefault(kvp => kvp.Value == progressionRoleName).Key;
            
            if (roleId == 0) yield break;

            var contingentRoleIds = DelubrumProgressionRoles.GetContingentRoles(roleId);
            foreach (var contingentRoleId in contingentRoleIds)
            {
                if (DelubrumProgressionRoles.Roles.TryGetValue(contingentRoleId, out var roleName))
                {
                    yield return roleName;
                }
            }
        }

        public bool ShouldProcessEncounter(int? difficulty)
        {
            // DRS Savage encounters have difficulty != 100
            return difficulty != 100;
        }
    }
}
