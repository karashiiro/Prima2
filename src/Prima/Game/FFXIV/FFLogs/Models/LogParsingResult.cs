using System.Collections.Generic;

namespace Prima.Game.FFXIV.FFLogs.Models
{
    /// <summary>
    /// Result of parsing a log for role assignments
    /// </summary>
    public class LogParsingResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<UserRoleAssignment> RoleAssignments { get; set; } = new();
        public List<string> MissedUsers { get; set; } = new();
        public bool HasAnyChanges => RoleAssignments.Count > 0;
    }

    /// <summary>
    /// Represents a role assignment for a specific user
    /// </summary>
    public class UserRoleAssignment
    {
        public LogUser User { get; set; }
        public List<RoleAction> RoleActions { get; set; } = new();
    }

    /// <summary>
    /// Represents a user found in the log
    /// </summary>
    public class LogUser
    {
        public int LogId { get; set; }
        public string CharacterName { get; set; }
        public string World { get; set; }
        public ulong? DiscordId { get; set; }
    }

    /// <summary>
    /// Represents an action to take on a role (add or remove)
    /// </summary>
    public class RoleAction
    {
        public string RoleName { get; set; }
        public RoleActionType ActionType { get; set; }
        public string Reason { get; set; }
    }

    public enum RoleActionType
    {
        Add,
        Remove
    }
}
