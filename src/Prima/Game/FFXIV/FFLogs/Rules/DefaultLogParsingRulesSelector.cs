using System;
using System.Linq;
using Prima.Models.FFLogs;
using Prima.Resources;

namespace Prima.Game.FFXIV.FFLogs.Rules
{
    public class DefaultLogParsingRulesSelector : ILogParsingRulesSelector
    {
        public ILogParsingRules GetParsingRules(LogInfo.ReportDataWrapper.ReportData.Report report)
        {
            foreach (var encounter in report.Fights)
            {
                if (string.IsNullOrWhiteSpace(encounter.Name))
                {
                    continue;
                }

                if (DelubrumProgressionRoles.Roles.Values.Any(roleName => roleName.Contains(encounter.Name)))
                {
                    return new DelubrumReginaeSavageRules();
                }
                else if (ForkedTowerRules.Roles.Values.Any(roleName => roleName.Contains(encounter.Name)))
                {
                    return new ForkedTowerRules();
                }
            }

            throw new InvalidOperationException("Could not determine parsing rules.");
        }
    }
}