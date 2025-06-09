using Prima.Models.FFLogs;

namespace Prima.Game.FFXIV.FFLogs.Rules
{
    public interface ILogParsingRulesSelector
    {
        ILogParsingRules GetParsingRules(LogInfo.ReportDataWrapper.ReportData.Report report);
    }
}