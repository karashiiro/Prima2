using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Prima.Game.FFXIV;
using Prima.Game.FFXIV.FFLogs;
using Prima.Game.FFXIV.FFLogs.Rules;
using Prima.Models.FFLogs;
using Prima.Resources;
using Prima.Tests.Mocks;

namespace Prima.Tests
{
    [TestFixture]
    public class LogParserServiceTests
    {
        private LogParserService _service;
        private MockFFLogsClient _mockFFLogsClient;
        private MockBatchActorMapper _mockActorMapper;
        private ILogParsingRules _logParsingRules;

        [SetUp]
        public void Setup()
        {
            _mockFFLogsClient = new MockFFLogsClient();
            _service = new LogParserService(_mockFFLogsClient);
            _mockActorMapper = new MockBatchActorMapper();
            _logParsingRules = new DelubrumReginaeSavageRules();
        }

        [Test]
        public async Task ReadLog_InvalidLogLink_ReturnsError()
        {
            var result = await _service.ReadLog("not-a-log-link", _mockActorMapper, _logParsingRules);

            Assert.That(result, Is.InstanceOf<LogParsingResult.Failure>());
            var failure = (LogParsingResult.Failure)result;
            Assert.That(failure.ErrorMessage, Is.EqualTo("That doesn't look like a log link!"));
        }

        [Test]
        public async Task ReadLog_PrivateLog_ReturnsError()
        {
            var testLog = CreatePrivateLog();
            _mockFFLogsClient.SetupLog(testLog);

            var result = await _service.ReadLog("https://www.fflogs.com/reports/abcde12345", _mockActorMapper,
                _logParsingRules);

            Assert.That(result, Is.InstanceOf<LogParsingResult.Failure>());
            var failure = (LogParsingResult.Failure)result;
            Assert.That(failure.ErrorMessage, Is.EqualTo("That log is private; please make it unlisted or public."));
        }

        [Test]
        public async Task ReadLog_ValidDRSLog_UsesDefaultRules()
        {
            // Setup a valid DRS log
            var testLog = CreateTestDRSLog();
            _mockFFLogsClient.SetupLog(testLog);
            _mockActorMapper.SetupUsers(new Dictionary<int, DiscordXIVUser>
            {
                { 1, new DiscordXIVUser { Name = "TestPlayer", World = "TestWorld", DiscordId = 12345 } },
            });

            var result = await _service.ReadLog("https://www.fflogs.com/reports/abcde12345", _mockActorMapper,
                _logParsingRules);

            Assert.That(result, Is.InstanceOf<LogParsingResult.Success>());
            var success = (LogParsingResult.Success)result;
            Assert.That(success.HasAnyChanges, Is.True);
            Assert.That(success.RoleAssignments.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task ReadLog_WithCustomRules_UsesProvidedRules()
        {
            var testLog = CreateTestDRSLog();
            _mockFFLogsClient.SetupLog(testLog);
            _mockActorMapper.SetupUsers(new Dictionary<int, DiscordXIVUser>
            {
                { 1, new DiscordXIVUser { Name = "TestPlayer", World = "TestWorld", DiscordId = 12345 } },
            });

            var customRules = new DelubrumReginaeSavageRules();
            var result = await _service.ReadLog("https://www.fflogs.com/reports/abcde12345", _mockActorMapper,
                customRules);

            Assert.That(result, Is.InstanceOf<LogParsingResult.Success>());
            var success = (LogParsingResult.Success)result;
            Assert.That(success.HasAnyChanges, Is.True);
        }

        [Test]
        public async Task ReadLog_TrinitySeeker_AssignsCorrectRoles()
        {
            var testLog = CreateTestLogWithEncounter("Trinity Seeker", true, 101);
            _mockFFLogsClient.SetupLog(testLog);
            _mockActorMapper.SetupUsers(new Dictionary<int, DiscordXIVUser>
            {
                { 1, new DiscordXIVUser { Name = "TestPlayer", World = "TestWorld", DiscordId = 12345 } },
            });

            var result = await _service.ReadLog("https://www.fflogs.com/reports/abcde12345", _mockActorMapper,
                _logParsingRules);

            Assert.That(result, Is.InstanceOf<LogParsingResult.Success>());
            var success = (LogParsingResult.Success)result;
            Assert.That(success.RoleAssignments.Count, Is.EqualTo(1));

            var assignment = success.RoleAssignments[0];
            var roleActions = assignment.RoleActions;

            // Should add Trinity Seeker Progression role
            Assert.That(roleActions.Any(ra =>
                ra.ActionType == LogParsingResult.RoleActionType.Add &&
                ra.RoleId == DelubrumProgressionRoles.Roles.First(kvp => kvp.Value == "Trinity Seeker Progression")
                    .Key));

            // Should add Queen's Guard Progression role (kill role)
            Assert.That(roleActions.Any(ra =>
                ra.ActionType == LogParsingResult.RoleActionType.Add &&
                ra.RoleId == DelubrumProgressionRoles.Roles.First(kvp => kvp.Value == "Queen's Guard Progression")
                    .Key));
        }

        [Test]
        public async Task ReadLog_TheQueenKill_RemovesProgressionRolesAndAddsClear()
        {
            var testLog = CreateTestLogWithEncounter("The Queen", true, 101);
            _mockFFLogsClient.SetupLog(testLog);
            _mockActorMapper.SetupUsers(new Dictionary<int, DiscordXIVUser>
            {
                { 1, new DiscordXIVUser { Name = "TestPlayer", World = "TestWorld", DiscordId = 12345 } },
            });

            var result = await _service.ReadLog("https://www.fflogs.com/reports/abcde12345", _mockActorMapper,
                _logParsingRules);

            Assert.That(result, Is.InstanceOf<LogParsingResult.Success>());
            var success = (LogParsingResult.Success)result;

            var assignment = success.RoleAssignments[0];
            var roleActions = assignment.RoleActions;

            // Should have remove actions for all progression roles
            var removeActions = roleActions.Where(ra => ra.ActionType == LogParsingResult.RoleActionType.Remove)
                .ToList();
            Assert.That(removeActions.Count, Is.GreaterThan(0));

            // Should add DRS Cleared role
            Assert.That(roleActions.Any(ra =>
                ra.ActionType == LogParsingResult.RoleActionType.Add &&
                ra.RoleId == DelubrumProgressionRoles.ClearedDelubrumSavage));
        }

        [Test]
        public async Task ReadLog_NormalMode_SkipsEncounter()
        {
            var testLog = CreateTestLogWithEncounter("Trinity Seeker", true, 100); // difficulty 100 = normal
            _mockFFLogsClient.SetupLog(testLog);
            _mockActorMapper.SetupUsers(new Dictionary<int, DiscordXIVUser>
            {
                { 1, new DiscordXIVUser { Name = "TestPlayer", World = "TestWorld", DiscordId = 12345 } },
            });

            var result = await _service.ReadLog("https://www.fflogs.com/reports/abcde12345", _mockActorMapper,
                _logParsingRules);

            Assert.That(result, Is.InstanceOf<LogParsingResult.Success>());
            var success = (LogParsingResult.Success)result;
            Assert.That(success.HasAnyChanges, Is.False);
        }

        [Test]
        public async Task ReadLog_UnknownEncounter_SkipsEncounter()
        {
            var testLog = CreateTestLogWithEncounter("Unknown Boss", true, 101);
            _mockFFLogsClient.SetupLog(testLog);
            _mockActorMapper.SetupUsers(new Dictionary<int, DiscordXIVUser>
            {
                { 1, new DiscordXIVUser { Name = "TestPlayer", World = "TestWorld", DiscordId = 12345 } },
            });

            var result = await _service.ReadLog("https://www.fflogs.com/reports/abcde12345", _mockActorMapper,
                _logParsingRules);

            Assert.That(result, Is.InstanceOf<LogParsingResult.Success>());
            var success = (LogParsingResult.Success)result;
            Assert.That(success.HasAnyChanges, Is.False);
        }

        [Test]
        public async Task ReadLog_MissedUsers_ReportsInResult()
        {
            var testLog = CreateTestDRSLog();
            _mockFFLogsClient.SetupLog(testLog);
            _mockActorMapper.SetupUsers(new Dictionary<int, DiscordXIVUser>()); // No users mapped

            var result = await _service.ReadLog("https://www.fflogs.com/reports/abcde12345", _mockActorMapper,
                _logParsingRules);

            Assert.That(result, Is.InstanceOf<LogParsingResult.Success>());
            var success = (LogParsingResult.Success)result;
            Assert.That(success.MissedUsers.Count, Is.EqualTo(1));
            Assert.That(success.MissedUsers[0].Name, Is.EqualTo("TestPlayer"));
        }

        private static LogInfo CreateTestDRSLog()
        {
            return CreateTestLogWithEncounter("Trinity Seeker", true, 101);
        }

        private static LogInfo CreateTestLogWithEncounter(string encounterName, bool? kill, int? difficulty)
        {
            return new LogInfo
            {
                Content = new LogInfo.ReportDataWrapper
                {
                    Data = new LogInfo.ReportDataWrapper.ReportData
                    {
                        ReportInfo = new LogInfo.ReportDataWrapper.ReportData.Report
                        {
                            Fights = new[]
                            {
                                new LogInfo.ReportDataWrapper.ReportData.Report.Fight
                                {
                                    Name = encounterName,
                                    Kill = kill,
                                    Difficulty = difficulty,
                                    FriendlyPlayers = new[] { 1 },
                                },
                            },
                            MasterData = new LogInfo.ReportDataWrapper.ReportData.Report.Master
                            {
                                Actors = new[]
                                {
                                    new LogInfo.ReportDataWrapper.ReportData.Report.Master.Actor
                                    {
                                        Id = 1,
                                        Name = "TestPlayer",
                                        Server = "TestWorld",
                                    },
                                },
                            },
                        },
                    },
                },
            };
        }

        private static LogInfo CreatePrivateLog()
        {
            return new LogInfo
            {
                Content = new LogInfo.ReportDataWrapper
                {
                    Data = new LogInfo.ReportDataWrapper.ReportData
                    {
                        ReportInfo = null,
                    },
                },
            };
        }
    }
}