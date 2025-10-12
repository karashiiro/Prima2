using System.Collections.Generic;
using Prima.Game.FFXIV;
using Prima.Game.FFXIV.FFLogs.Rules;

namespace Prima.Models.FFLogs
{
    public class LogParsingResult
    {
        public static LogParsingResult OfError(string error)
        {
            return new Failure
            {
                ErrorMessage = error,
            };
        }

        public class Success : LogParsingResult
        {
            public List<UserRoleAssignment> RoleAssignments { get; init; } = new();
            public List<LogInfo.ReportDataWrapper.ReportData.Report.Master.Actor> MissedUsers { get; init; } = new();
            public bool HasAnyChanges => RoleAssignments.Count > 0;
            public ILogParsingRules Rules { get; init; }
        }

        public class Failure : LogParsingResult
        {
            public string ErrorMessage { get; init; }
        }

        public class UserRoleAssignment
        {
            public DiscordXIVUser User { get; init; }
            public List<RoleAction> RoleActions { get; init; } = new();
        }

        public class RoleAction
        {
            public ulong RoleId { get; init; }
            public RoleActionType ActionType { get; init; }
        }

        public enum RoleActionType
        {
            Add,
            Remove,
        }
    }
}