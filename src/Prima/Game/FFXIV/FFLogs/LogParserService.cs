using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prima.Game.FFXIV.FFLogs.Rules;
using Prima.Models.FFLogs;
using Serilog;

namespace Prima.Game.FFXIV.FFLogs
{
    public class LogParserService : ILogParserService
    {
        private readonly IFFLogsClient _ffLogs;
        private readonly ILogParsingRulesSelector _logParsingRulesSelector;
        private readonly ILogger<LogParserService> _logger;

        public LogParserService(IFFLogsClient ffLogs, ILogParsingRulesSelector logParsingRulesSelector,
            ILogger<LogParserService> logger)
        {
            _ffLogs = ffLogs;
            _logParsingRulesSelector = logParsingRulesSelector;
            _logger = logger;
        }

        public async Task<LogParsingResult> ReadLog(string logLink, IBatchActorMapper actorMapper)
        {
            // Log validation
            var validationResult = await ValidateLog(logLink);
            if (validationResult is LogValidationResult.Failure validationFailure)
            {
                return LogParsingResult.OfError(validationFailure.Message);
            }

            var res = ((LogValidationResult.Success)validationResult).Report;

            try
            {
                // Extract fight information to determine the most appropriate ruleset
                var rules = _logParsingRulesSelector.GetParsingRules(res);

                // Read log
                return await ReadLog(logLink, actorMapper, rules);
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(e, "Failed to read log");
                return LogParsingResult.OfError(e.Message);
            }
        }

        public async Task<LogParsingResult> ReadLog(string logLink, IBatchActorMapper actorMapper,
            ILogParsingRules rules)
        {
            _logger.LogInformation("Parsing log with ruleset {RulesetName}", rules.GetType().Name);
            
            // Log validation
            var validationResult = await ValidateLog(logLink);
            if (validationResult is LogValidationResult.Failure validationFailure)
            {
                return LogParsingResult.OfError(validationFailure.Message);
            }

            var res = ((LogValidationResult.Success)validationResult).Report;

            // Extract encounters and fight members
            var encounters = res.Fights
                .Where(f => f.Kill != null && f.FriendlyPlayers != null);

            var actors = res.MasterData.Actors
                .Where(a => a.Server != null)
                .ToList();
            var users = await actorMapper.GetUsersFromActors(actors);
            var missedUsers = new List<LogInfo.ReportDataWrapper.ReportData.Report.Master.Actor>();

            // Record kill roles for each fight member
            var roleActionsByUsers = new Dictionary<DiscordXIVUser, List<LogParsingResult.RoleAction>>();
            foreach (var encounter in encounters)
            {
                if (!rules.ShouldProcessEncounter(encounter.Difficulty))
                {
                    _logger.LogWarning("Encounter {Encounter} should not be processed based on difficulty {Difficulty}",
                        encounter.Name, encounter.Difficulty);
                    continue;
                }

                var progressionRoleName = rules.GetProgressionRoleName(encounter.Name);
                if (progressionRoleName == null)
                {
                    _logger.LogWarning("No progression role mapping found for encounter: {EncounterName}",
                        encounter.Name);
                    continue;
                }

                var killRoleId = rules.GetKillRoleId(progressionRoleName);
                if (killRoleId == 0)
                {
                    _logger.LogWarning("No kill role ID found for progression role: {ProgressionRole}",
                        progressionRoleName);
                    continue;
                }

                var contingentRoleIds = rules.GetContingentRoleIds(killRoleId)
                    .Except(new[] { killRoleId })
                    .ToList();

                foreach (var id in encounter.FriendlyPlayers)
                {
                    if (!users.TryGetValue(id, out var discordUser))
                    {
                        var actor = actors.Find(a => a.Id == id);
                        if (actor != null)
                        {
                            _logger.LogWarning("Can't find Discord user for actor {ActorId}: ({World}) {CharacterName}",
                                actor.Id, actor.Server, actor.Name);
                            missedUsers.Add(actor);
                        }

                        continue;
                    }

                    if (discordUser == null)
                    {
                        _logger.LogWarning("Can't find Discord user {DiscordId}", id);
                        continue;
                    }

                    var roleActions =
                        roleActionsByUsers.GetValueOrDefault(discordUser, new List<LogParsingResult.RoleAction>());
                    roleActionsByUsers.TryAdd(discordUser, roleActions);

                    if (killRoleId == rules.FinalClearRoleId && encounter.Kill == true)
                    {
                        _logger.LogInformation("Adding clear role for {EncounterName} to Discord user {DiscordName}",
                            encounter.Name, discordUser.ToString());

                        // Remove all contingent roles
                        roleActions.AddRange(contingentRoleIds
                            .Select(progRoleId => new LogParsingResult.RoleAction
                            {
                                ActionType = LogParsingResult.RoleActionType.Remove,
                                RoleId = progRoleId,
                            }));

                        // Add the clear role
                        roleActions.Add(new LogParsingResult.RoleAction
                        {
                            ActionType = LogParsingResult.RoleActionType.Add,
                            RoleId = killRoleId,
                        });
                    }
                    else
                    {
                        _logger.LogInformation("Adding prog roles for {EncounterName} to Discord user {DiscordName}",
                            encounter.Name, discordUser.ToString());

                        // Give all contingent roles as well as the clear role for the fight if cleared
                        roleActions.AddRange(contingentRoleIds
                            .Select(progRoleId => new LogParsingResult.RoleAction
                            {
                                ActionType = LogParsingResult.RoleActionType.Add,
                                RoleId = progRoleId,
                            }));

                        if (encounter.Kill == true)
                        {
                            _logger.LogInformation("Adding kill role for {EncounterName} to Discord user {DiscordName}",
                                encounter.Name, discordUser.ToString());

                            roleActions.Add(new LogParsingResult.RoleAction
                            {
                                ActionType = LogParsingResult.RoleActionType.Add,
                                RoleId = killRoleId,
                            });
                        }
                    }
                }
            }

            // Reshape into role assignments
            var roleAssignments = roleActionsByUsers
                .Select(kvp => new LogParsingResult.UserRoleAssignment
                {
                    User = kvp.Key,
                    RoleActions = kvp.Value,
                })
                .ToList();

            _logger.LogInformation("Successfully parsed log with ruleset {RulesetName}", rules.GetType().Name);
            return new LogParsingResult.Success
            {
                RoleAssignments = roleAssignments,
                MissedUsers = missedUsers,
                Rules = rules,
            };
        }

        private async Task<LogValidationResult> ValidateLog(string logLink)
        {
            var logMatch = FFLogsUtils.LogLinkToIdRegex.Match(logLink);
            if (!logMatch.Success)
            {
                return LogValidationResult.OfError("That doesn't look like a log link!");
            }

            var logId = logMatch.Value;
            var req = FFLogsUtils.BuildLogRequest(logId);
            var res = (await _ffLogs.MakeGraphQLRequest<LogInfo>(req)).Content.Data.ReportInfo;
            if (res == null)
            {
                return LogValidationResult.OfError("That log is private; please make it unlisted or public.");
            }

            return new LogValidationResult.Success
            {
                Report = res,
            };
        }

        private class LogValidationResult
        {
            public static LogValidationResult OfError(string message)
            {
                return new Failure
                {
                    Message = message,
                };
            }

            public class Failure : LogValidationResult
            {
                public string Message { get; init; }
            }

            public class Success : LogValidationResult
            {
                public LogInfo.ReportDataWrapper.ReportData.Report Report { get; init; }
            }
        }
    }
}