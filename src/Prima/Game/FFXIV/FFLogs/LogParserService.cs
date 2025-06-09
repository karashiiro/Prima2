using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prima.Models.FFLogs;
using Prima.Resources;
using Serilog;

namespace Prima.Game.FFXIV.FFLogs
{
    public class LogParserService : ILogParserService
    {
        private readonly FFLogsClient _ffLogs;

        public LogParserService(FFLogsClient ffLogs)
        {
            _ffLogs = ffLogs;
        }

        public async Task<LogParsingResult> ReadLog(string logLink, IBatchActorMapper actorMapper)
        {
            // Log validation
            var logMatch = FFLogsUtils.LogLinkToIdRegex.Match(logLink);
            if (!logMatch.Success)
            {
                return LogParsingResult.OfError("That doesn't look like a log link!");
            }

            var logId = logMatch.Value;
            var req = FFLogsUtils.BuildLogRequest(logId);
            var res = (await _ffLogs.MakeGraphQLRequest<LogInfo>(req)).Content.Data.ReportInfo;
            if (res == null)
            {
                return LogParsingResult.OfError("That log is private; please make it unlisted or public.");
            }

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
                if (encounter.Difficulty == 100)
                {
                    Log.Warning("Encounter {Encounter} does not appear to be a Savage encounter", encounter.Name);
                    continue;
                }

                // TODO: Extract logic to allow reusing this for Forked Tower
                var roleName = encounter.Name;
                if (roleName == "The Queen's Guard")
                    roleName = "Queen's Guard";
                roleName += " Progression";

                var killRoleId = DelubrumProgressionRoles.GetKillRole(roleName);
                var contingentRoleIds = DelubrumProgressionRoles.GetContingentRoles(killRoleId)
                    .ToList();

                foreach (var id in encounter.FriendlyPlayers)
                {
                    if (!users.TryGetValue(id, out var discordUser))
                    {
                        var actor = actors.Find(a => a.Id == id);
                        if (actor != null)
                        {
                            missedUsers.Add(actor);
                        }

                        continue;
                    }

                    if (discordUser == null)
                    {
                        continue;
                    }

                    var roleActions =
                        roleActionsByUsers.GetValueOrDefault(discordUser, new List<LogParsingResult.RoleAction>());
                    roleActionsByUsers.TryAdd(discordUser, roleActions);

                    // TODO: Extract logic to allow reusing this for Forked Tower
                    if (killRoleId == DelubrumProgressionRoles.ClearedDelubrumSavage && encounter.Kill == true)
                    {
                        // Remove all contingent roles
                        roleActions.AddRange(contingentRoleIds.Select(progRoleId => new LogParsingResult.RoleAction
                            { ActionType = LogParsingResult.RoleActionType.Remove, RoleId = progRoleId }));

                        // Give everyone the clear role if they cleared DRS
                        roleActions.Add(new LogParsingResult.RoleAction
                        {
                            ActionType = LogParsingResult.RoleActionType.Add,
                            RoleId = killRoleId,
                        });
                    }
                    else
                    {
                        // Give all contingent roles as well as the clear role for the fight
                        roleActions.AddRange(contingentRoleIds.Select(progRoleId => new LogParsingResult.RoleAction
                            { ActionType = LogParsingResult.RoleActionType.Add, RoleId = progRoleId }));

                        if (encounter.Kill == true)
                        {
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
            return new LogParsingResult.Success
            {
                RoleAssignments = roleAssignments,
                MissedUsers = missedUsers,
            };
        }
    }
}