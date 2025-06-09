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
                if (DelubrumProgressionRoles.Roles.Values.Any(roleName => roleName.Contains(encounter.Name)))
                {
                    return new DelubrumReginaeSavageRules();
                }
            }

            throw new InvalidOperationException("Could not determine parsing rules.");
        }
    }
}