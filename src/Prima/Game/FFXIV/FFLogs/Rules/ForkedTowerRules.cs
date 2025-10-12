using System.Collections.Generic;
using System.Linq;

namespace Prima.Game.FFXIV.FFLogs.Rules
{
    public class ForkedTowerRules : ILogParsingRules
    {
        public ulong FinalClearRoleId => ClearedForkedTower;

        public const ulong DemonTabletProgression = 1381806320612806757;
        public const ulong DeadStarsProgression = 1381804104199831652;
        public const ulong MarbleDragonProgression = 1381804200941588518;
        public const ulong MagitaurProgression = 1381804261297619154;
        public const ulong ClearedForkedTower = 1381433968431202424;

        public static readonly Dictionary<ulong, string> Roles = new()
        {
            {DemonTabletProgression, "Demon Tablet Progression"},
            {DeadStarsProgression, "Dead Stars Progression"},
            {MarbleDragonProgression, "Marble Dragon Progression"},
            {MagitaurProgression, "Magitaur Progression"},
        };

        public string GetProgressionRoleName(string encounterName)
        {
            var roleName = encounterName + " Progression";
            return Roles.Any(kvp => kvp.Value == roleName) ? roleName : null;
        }

        public ulong GetKillRoleId(string roleName)
        {
            return roleName switch
            {
                "Demon Tablet Progression" => DeadStarsProgression,
                "Dead Stars Progression" => MarbleDragonProgression,
                "Marble Dragon Progression" => MagitaurProgression,
                "Magitaur Progression" => FinalClearRoleId,
                _ => 0,
            };
        }

        public IEnumerable<ulong> GetContingentRoleIds(ulong roleId)
        {
            var baseList = new[] { MagitaurProgression, MarbleDragonProgression, DeadStarsProgression, DemonTabletProgression };
            return roleId switch
            {
                ClearedForkedTower => baseList,
                MagitaurProgression => baseList,
                MarbleDragonProgression => baseList[1..],
                DeadStarsProgression => baseList[2..],
                DemonTabletProgression => baseList[3..],
                _ => new List<ulong>(),
            };
        }

        public bool ShouldProcessEncounter(int? difficulty)
        {
            // No Normal/Savage split ike DRS
            return true;
        }
    }
}