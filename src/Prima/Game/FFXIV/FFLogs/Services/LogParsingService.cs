using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prima.Game.FFXIV.FFLogs.Models;
using Prima.Models.FFLogs;
using Prima.Services;

namespace Prima.Game.FFXIV.FFLogs.Services
{
    public class LogParsingService : ILogParsingService
    {
        private readonly ILogger<LogParsingService> _logger;
        private readonly IFFLogsClient _ffLogsClient;
        private readonly IDbService _dbService;

        public LogParsingService(
            ILogger<LogParsingService> logger, 
            IFFLogsClient ffLogsClient, 
            IDbService dbService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ffLogsClient = ffLogsClient ?? throw new ArgumentNullException(nameof(ffLogsClient));
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        }

        public async Task<LogParsingResult> ParseLogAsync(LogParsingRequest request)
        {
            if (request?.Rules == null)
            {
                return new LogParsingResult
                {
                    Success = false,
                    ErrorMessage = "Invalid parsing request"
                };
            }

            try
            {
                _logger.LogInformation("Starting log parsing for URL: {LogUrl}", request.LogUrl);

                var logMatch = FFLogsUtils.LogLinkToIdRegex.Match(request.LogUrl);
                if (!logMatch.Success)
                {
                    return new LogParsingResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid log URL format"
                    };
                }

                var logId = logMatch.Value;
                var graphqlRequest = FFLogsUtils.BuildLogRequest(logId);
                var response = await _ffLogsClient.MakeGraphQLRequest<LogInfo>(graphqlRequest);
                
                if (response?.Content?.Data?.ReportInfo == null)
                {
                    return new LogParsingResult
                    {
                        Success = false,
                        ErrorMessage = "Log is private or does not exist"
                    };
                }

                return await ProcessLogData(response.Content.Data.ReportInfo, request.Rules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing log: {LogUrl}", request.LogUrl);
                return new LogParsingResult
                {
                    Success = false,
                    ErrorMessage = $"Error parsing log: {ex.Message}"
                };
            }
        }

        private async Task<LogParsingResult> ProcessLogData(
            LogInfo.ReportDataWrapper.ReportData.Report reportInfo,
            ILogParsingRules rules)
        {
            var result = new LogParsingResult { Success = true };

            // Get relevant encounters (kills only)
            var encounters = reportInfo.Fights
                .Where(f => f.Kill != null && 
                           f.FriendlyPlayers != null && 
                           rules.ShouldProcessEncounter(f.Difficulty))
                .ToList();

            if (!encounters.Any())
            {
                _logger.LogInformation("No valid encounters found in log");
                return result;
            }

            // Build user lookup
            var logUsers = await BuildUserLookup(reportInfo.MasterData.Actors, result);

            // Process each encounter
            foreach (var encounter in encounters)
            {
                await ProcessEncounter(encounter, logUsers, rules, result);
            }

            _logger.LogInformation("Log parsing completed. Found {AssignmentCount} role assignments", 
                result.RoleAssignments.Count);

            return result;
        }

        private async Task<Dictionary<int, LogUser>> BuildUserLookup(
            LogInfo.ReportDataWrapper.ReportData.Report.Master.Actor[] actors,
            LogParsingResult result)
        {
            var logUsers = new Dictionary<int, LogUser>();

            foreach (var actor in actors.Where(a => a.Server != null))
            {
                var logUser = new LogUser
                {
                    LogId = actor.Id,
                    CharacterName = actor.Name,
                    World = actor.Server
                };

                // Try to find Discord user in database
                var dbUser = _dbService.Users.FirstOrDefault(u =>
                    string.Equals(u.Name, actor.Name, StringComparison.InvariantCultureIgnoreCase) &&
                    string.Equals(u.World, actor.Server, StringComparison.InvariantCultureIgnoreCase));

                if (dbUser != null)
                {
                    logUser.DiscordId = dbUser.DiscordId;
                    logUsers[actor.Id] = logUser;
                }
                else
                {
                    result.MissedUsers.Add($"({actor.Server}) {actor.Name}");
                    _logger.LogWarning("Could not find Discord user for character: {Character} on {World}", 
                        actor.Name, actor.Server);
                }
            }

            _logger.LogInformation("Built user lookup with {RegisteredUsers} registered users and {MissedUsers} missed users",
                logUsers.Count, result.MissedUsers.Count);

            return logUsers;
        }

        private async Task ProcessEncounter(
            LogInfo.ReportDataWrapper.ReportData.Report.Fight encounter,
            Dictionary<int, LogUser> logUsers,
            ILogParsingRules rules,
            LogParsingResult result)
        {
            var progressionRoleName = rules.GetProgressionRoleName(encounter.Name);
            if (string.IsNullOrEmpty(progressionRoleName))
            {
                _logger.LogWarning("No progression role mapping found for encounter: {EncounterName}", encounter.Name);
                return;
            }

            _logger.LogInformation("Processing encounter: {EncounterName} -> {ProgressionRole}", 
                encounter.Name, progressionRoleName);

            var clearRoleName = rules.GetClearRoleName(progressionRoleName);
            var contingentRoles = rules.GetContingentRoles(progressionRoleName).ToList();

            foreach (var playerId in encounter.FriendlyPlayers)
            {
                if (!logUsers.TryGetValue(playerId, out var logUser) || !logUser.DiscordId.HasValue)
                {
                    continue;
                }

                var assignment = result.RoleAssignments.FirstOrDefault(ra => ra.User.DiscordId == logUser.DiscordId);
                if (assignment == null)
                {
                    assignment = new UserRoleAssignment { User = logUser };
                    result.RoleAssignments.Add(assignment);
                }

                // Handle final clear (special case for content like DRS)
                if (!string.IsNullOrEmpty(rules.FinalClearRoleName) && 
                    clearRoleName == rules.FinalClearRoleName && 
                    encounter.Kill == true)
                {
                    // Remove all progression roles when getting final clear
                    foreach (var contingentRole in contingentRoles)
                    {
                        assignment.RoleActions.Add(new RoleAction
                        {
                            RoleName = contingentRole,
                            ActionType = RoleActionType.Remove,
                            Reason = $"Cleared {encounter.Name}"
                        });
                    }

                    // Add the final clear role
                    assignment.RoleActions.Add(new RoleAction
                    {
                        RoleName = rules.FinalClearRoleName,
                        ActionType = RoleActionType.Add,
                        Reason = $"Cleared {encounter.Name}"
                    });
                }
                else
                {
                    // Add progression roles
                    foreach (var contingentRole in contingentRoles)
                    {
                        assignment.RoleActions.Add(new RoleAction
                        {
                            RoleName = contingentRole,
                            ActionType = RoleActionType.Add,
                            Reason = $"Participated in {encounter.Name}"
                        });
                    }

                    // Add clear role if killed
                    if (encounter.Kill == true && !string.IsNullOrEmpty(clearRoleName))
                    {
                        assignment.RoleActions.Add(new RoleAction
                        {
                            RoleName = clearRoleName,
                            ActionType = RoleActionType.Add,
                            Reason = $"Cleared {encounter.Name}"
                        });
                    }
                }
            }
        }
    }
}
