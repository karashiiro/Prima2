using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Prima.Game.FFXIV;
using Prima.Game.FFXIV.FFLogs;
using Prima.Game.FFXIV.FFLogs.Models;
using Prima.Game.FFXIV.FFLogs.Rules;
using Prima.Game.FFXIV.FFLogs.Services;
using Prima.Models.FFLogs;
using Prima.Tests.Mocks;

namespace Prima.Tests.Game.FFXIV.FFLogs.Services
{
    [TestFixture]
    public class LogParsingIntegrationTests
    {
        private MemoryDb _db;
        private Mock<IFFLogsClient> _mockFFLogsClient;
        private Mock<ILogger<LogParsingService>> _mockLogger;
        private LogParsingService _service;

        [SetUp]
        public void Setup()
        {
            _db = new MemoryDb();
            _mockFFLogsClient = new Mock<IFFLogsClient>();
            _mockLogger = new Mock<ILogger<LogParsingService>>();
            
            _service = new LogParsingService(_mockLogger.Object, _mockFFLogsClient.Object, _db);
        }

        [Test]
        public async Task LogParsingService_WithDRSRules_ParsesCorrectly()
        {
            // Arrange - Add test user to database
            var testUser = new DiscordXIVUser
            {
                DiscordId = 12345,
                Name = "Test Player", 
                World = "TestWorld",
                LodestoneId = "123456789"
            };
            await _db.AddUser(testUser);

            // Set up mock log data
            var logData = CreateDRSSampleLog();
            _mockFFLogsClient.Setup(x => x.MakeGraphQLRequest<LogInfo>(It.IsAny<string>()))
                .ReturnsAsync(logData);

            var request = new LogParsingRequest
            {
                LogUrl = "https://www.fflogs.com/reports/abc123def456",
                Rules = new DelubrumReginaeSavageRules()
            };

            // Act
            var result = await _service.ParseLogAsync(request);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.RoleAssignments.Count);
            
            var assignment = result.RoleAssignments[0];
            Assert.AreEqual(12345UL, assignment.User.DiscordId);
            Assert.IsNotEmpty(assignment.RoleActions);
            
            // Verify that Trinity Seeker progression roles are assigned
            Assert.IsTrue(assignment.RoleActions.Any(ra => 
                ra.RoleName == "Trinity Seeker Progression" && 
                ra.ActionType == RoleActionType.Add));
        }

        [Test]
        public async Task LogParsingService_WithBaldesionArsenalRules_ParsesCorrectly()
        {
            // Arrange - Add test user to database
            var testUser = new DiscordXIVUser
            {
                DiscordId = 54321,
                Name = "BA Player",
                World = "TestWorld",
                LodestoneId = "987654321"
            };
            await _db.AddUser(testUser);

            // Set up mock log data for BA
            var logData = CreateBASampleLog();
            _mockFFLogsClient.Setup(x => x.MakeGraphQLRequest<LogInfo>(It.IsAny<string>()))
                .ReturnsAsync(logData);

            var request = new LogParsingRequest
            {
                LogUrl = "https://www.fflogs.com/reports/xyz789abc123",
                Rules = new BaldesionArsenalRules()
            };

            // Act
            var result = await _service.ParseLogAsync(request);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.RoleAssignments.Count);
            
            var assignment = result.RoleAssignments[0];
            Assert.AreEqual(54321UL, assignment.User.DiscordId);
            
            // Verify BA specific roles are assigned
            Assert.IsTrue(assignment.RoleActions.Any(ra => 
                ra.RoleName == "Art Progression" && 
                ra.ActionType == RoleActionType.Add));
            Assert.IsTrue(assignment.RoleActions.Any(ra => 
                ra.RoleName == "BA Participant" && 
                ra.ActionType == RoleActionType.Add));
        }

        [Test]
        public async Task LogParsingService_WithMissedUsers_ReportsCorrectly()
        {
            // Arrange - Don't add any users to database, so all will be missed
            var logData = CreateDRSSampleLog();
            _mockFFLogsClient.Setup(x => x.MakeGraphQLRequest<LogInfo>(It.IsAny<string>()))
                .ReturnsAsync(logData);

            var request = new LogParsingRequest
            {
                LogUrl = "https://www.fflogs.com/reports/abc123def456",
                Rules = new DelubrumReginaeSavageRules()
            };

            // Act
            var result = await _service.ParseLogAsync(request);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.RoleAssignments.Count);
            Assert.AreEqual(1, result.MissedUsers.Count);
            Assert.AreEqual("(TestWorld) Test Player", result.MissedUsers[0]);
        }

        private static LogInfo CreateDRSSampleLog()
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
                                    Name = "Trinity Seeker",
                                    Kill = true,
                                    FriendlyPlayers = new[] { 1 },
                                    Difficulty = 101 // Savage difficulty
                                }
                            },
                            MasterData = new LogInfo.ReportDataWrapper.ReportData.Report.Master
                            {
                                Actors = new[]
                                {
                                    new LogInfo.ReportDataWrapper.ReportData.Report.Master.Actor
                                    {
                                        Id = 1,
                                        Name = "Test Player",
                                        Server = "TestWorld"
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        private static LogInfo CreateBASampleLog()
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
                                    Name = "Art",
                                    Kill = true,
                                    FriendlyPlayers = new[] { 1 },
                                    Difficulty = 100 // BA doesn't have difficulty restrictions
                                }
                            },
                            MasterData = new LogInfo.ReportDataWrapper.ReportData.Report.Master
                            {
                                Actors = new[]
                                {
                                    new LogInfo.ReportDataWrapper.ReportData.Report.Master.Actor
                                    {
                                        Id = 1,
                                        Name = "BA Player",
                                        Server = "TestWorld"
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
