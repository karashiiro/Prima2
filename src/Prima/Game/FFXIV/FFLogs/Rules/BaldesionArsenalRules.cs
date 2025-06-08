using System.Collections.Generic;
using Prima.Game.FFXIV.FFLogs.Models;

namespace Prima.Game.FFXIV.FFLogs.Rules
{
    /// <summary>
    /// Parsing rules specific to The Baldesion Arsenal
    /// </summary>
    public class BaldesionArsenalRules : ILogParsingRules
    {
        public string FinalClearRoleName => null; // BA doesn't have a single final clear role

        public string GetProgressionRoleName(string encounterName)
        {
            return encounterName switch
            {
                "Art" => "Art Progression",
                "Owain" => "Owain Progression", 
                "Raiden" => "Raiden Progression",
                "Absolute Virtue" => "AV Progression",
                "Ozma" => "Ozma Progression",
                _ => null
            };
        }

        public string GetClearRoleName(string progressionRoleName)
        {
            return progressionRoleName switch
            {
                "Art Progression" => "Art Clear",
                "Owain Progression" => "Owain Clear",
                "Raiden Progression" => "Raiden Clear", 
                "AV Progression" => "AV Clear",
                "Ozma Progression" => "Ozma Clear",
                _ => null
            };
        }

        public IEnumerable<string> GetContingentRoles(string progressionRoleName)
        {
            // For BA, everyone who gets a progression role also gets participant
            yield return progressionRoleName;
            yield return "BA Participant";
        }

        public bool ShouldProcessEncounter(int? difficulty)
        {
            // BA processes all encounters regardless of difficulty
            return true;
        }
    }
}
