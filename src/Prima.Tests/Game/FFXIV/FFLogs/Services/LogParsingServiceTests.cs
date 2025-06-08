using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Prima.Game.FFXIV;
using Prima.Game.FFXIV.FFLogs;
using Prima.Game.FFXIV.FFLogs.Models;
using Prima.Game.FFXIV.FFLogs.Services;
using Prima.Models.FFLogs;
using Prima.Services;

namespace Prima.Tests.Game.FFXIV.FFLogs.Services
{
    [TestFixture]
    public class LogParsingServiceTests
    {
        private Mock<ILogger<LogParsingService>> _mockLogger;
        private Mock<IFFLogsClient> _mockFFLogsClient;
        private Mock<IDbService> _mockDbService;
        private LogParsingService _service;
        private Mock<ILogParsingRules> _mockRules;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<LogParsingService>>();
            _mockFFLogsClient = new Mock<IFFLogsClient>();
            _mockDbService = new Mock<IDbService>();
            _mockRules = new Mock<ILogParsingRules>();

            _service = new LogParsingService(_mockLogger.Object, _mockFFLogsClient.Object, _mockDbService.Object);
        }

        [Test]
        public async Task ParseLogAsync_WithInvalidRequest_ReturnsFailure()
        {
            // Arrange
            LogParsingRequest request = null;

            // Act
            var result = await _service.ParseLogAsync(request);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Invalid parsing request", result.ErrorMessage);
        }

        [Test]
        public async Task ParseLogAsync_WithInvalidLogUrl_ReturnsFailure()
        {
            // Arrange
            var request = new LogParsingRequest
            {
                LogUrl = "invalid-url",
                Rules = _mockRules.Object
            };

            // Act
            var result = await _service.ParseLogAsync(request);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Invalid log URL format", result.ErrorMessage);
        }

        [Test]
        public async Task ParseLogAsync_WithPrivateLog_ReturnsFailure()
        {
            // Arrange
            var request = new LogParsingRequest
            {
                LogUrl = "https://www.fflogs.com/reports/abc123def456",
                Rules = _mockRules.Object
            };

            _mockFFLogsClient.Setup(x => x.MakeGraphQLRequest<LogInfo>(It.IsAny<string>()))
                .ReturnsAsync(new LogInfo { Content = null });

            // Act
            var result = await _service.ParseLogAsync(request);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Log is private or does not exist", result.ErrorMessage);
        }

        [Test]
        public async Task ParseLogAsync_WithValidLog_ProcessesEncounters()
        {
            // Arrange
            var request = new LogParsingRequest
            {
                LogUrl = "https://www.fflogs.com/reports/abc123def456",
                Rules = _mockRules.Object
            };

            var logData = CreateSampleLogData();
            var mockUsers = new List<DiscordXIVUser>
            {
                new() { Name = "TestPlayer", World = "TestWorld", DiscordId = 12345 }
            }.AsQueryable();

            _mockDbService.Setup(x => x.Users).Returns(mockUsers);
            _mockFFLogsClient.Setup(x => x.MakeGraphQLRequest<LogInfo>(It.IsAny<string>()))
                .ReturnsAsync(logData);

            _mockRules.Setup(x => x.ShouldProcessEncounter(It.IsAny<int?>())).Returns(true);
            _mockRules.Setup(x => x.GetProgressionRoleName("Test Boss")).Returns("Test Progression");
            _mockRules.Setup(x => x.GetClearRoleName("Test Progression")).Returns("Test Clear");
            _mockRules.Setup(x => x.GetContingentRoles("Test Progression"))
                .Returns(new[] { "Test Progression", "Test Participant" });

            // Act
            var result = await _service.ParseLogAsync(request);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.RoleAssignments.Count);
            Assert.AreEqual(12345UL, result.RoleAssignments[0].User.DiscordId);
            Assert.AreEqual(3, result.RoleAssignments[0].RoleActions.Count); // 2 contingent + 1 clear
        }

        [Test]
        public async Task ParseLogAsync_WithFinalClearRole_RemovesProgressionRoles()
        {
            // Arrange
            var request = new LogParsingRequest
            {
                LogUrl = "https://www.fflogs.com/reports/abc123def456",
                Rules = _mockRules.Object
            };

            var logData = CreateSampleLogData();
            var mockUsers = new List<DiscordXIVUser>
            {
                new() { Name = "TestPlayer", World = "TestWorld", DiscordId = 12345 }
            }.AsQueryable();

            _mockDbService.Setup(x => x.Users).Returns(mockUsers);
            _mockFFLogsClient.Setup(x => x.MakeGraphQLRequest<LogInfo>(It.IsAny<string>()))
                .ReturnsAsync(logData);

            _mockRules.Setup(x => x.ShouldProcessEncounter(It.IsAny<int?>())).Returns(true);
            _mockRules.Setup(x => x.GetProgressionRoleName("Test Boss")).Returns("Test Progression");
            _mockRules.Setup(x => x.GetClearRoleName("Test Progression")).Returns("Final Clear");
            _mockRules.Setup(x => x.FinalClearRoleName).Returns("Final Clear");
            _mockRules.Setup(x => x.GetContingentRoles("Test Progression"))
                .Returns(new[] { "Test Progression", "Test Participant" });

            // Act
            var result = await _service.ParseLogAsync(request);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.RoleAssignments.Count);
            
            var roleActions = result.RoleAssignments[0].RoleActions;
            Assert.AreEqual(3, roleActions.Count);
            
            // Should have 2 remove actions and 1 add action
            Assert.AreEqual(2, roleActions.Count(ra => ra.ActionType == RoleActionType.Remove));
            Assert.AreEqual(1, roleActions.Count(ra => ra.ActionType == RoleActionType.Add));
            Assert.IsTrue(roleActions.Any(ra => ra.RoleName == "Final Clear" && ra.ActionType == RoleActionType.Add));
        }

        [Test]
        public async Task ParseLogAsync_WithMissedUsers_ReportsMissedUsers()
        {
            // Arrange
            var request = new LogParsingRequest
            {
                LogUrl = "https://www.fflogs.com/reports/abc123def456",
                Rules = _mockRules.Object
            };

            var logData = CreateSampleLogData();
            var mockUsers = new List<DiscordXIVUser>().AsQueryable(); // Empty - no registered users

            _mockDbService.Setup(x => x.Users).Returns(mockUsers);
            _mockFFLogsClient.Setup(x => x.MakeGraphQLRequest<LogInfo>(It.IsAny<string>()))
                .ReturnsAsync(logData);

            _mockRules.Setup(x => x.ShouldProcessEncounter(It.IsAny<int?>())).Returns(true);

            // Act
            var result = await _service.ParseLogAsync(request);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.RoleAssignments.Count);
            Assert.AreEqual(1, result.MissedUsers.Count);
            Assert.AreEqual("(TestWorld) TestPlayer", result.MissedUsers[0]);
        }

        [Test]
        public async Task ParseLogAsync_WithNoValidEncounters_ReturnsEmptyResult()
        {
            // Arrange
            var request = new LogParsingRequest
            {
                LogUrl = "https://www.fflogs.com/reports/abc123def456",
                Rules = _mockRules.Object
            };

            var logData = CreateSampleLogData();
            logData.Content.Data.ReportInfo.Fights[0].Kill = null; // Make it not a kill

            _mockFFLogsClient.Setup(x => x.MakeGraphQLRequest<LogInfo>(It.IsAny<string>()))
                .ReturnsAsync(logData);

            _mockRules.Setup(x => x.ShouldProcessEncounter(It.IsAny<int?>())).Returns(false);

            // Act
            var result = await _service.ParseLogAsync(request);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.RoleAssignments.Count);
            Assert.IsFalse(result.HasAnyChanges);
        }

        [Test]
        public async Task ParseLogAsync_WithException_ReturnsFailure()
        {
            // Arrange
            var request = new LogParsingRequest
            {
                LogUrl = "https://www.fflogs.com/reports/abc123def456",
                Rules = _mockRules.Object
            };

            _mockFFLogsClient.Setup(x => x.MakeGraphQLRequest<LogInfo>(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Network error"));

            // Act
            var result = await _service.ParseLogAsync(request);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.ErrorMessage.Contains("Network error"));
        }

        private static LogInfo CreateSampleLogData()
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
                                    Name = "Test Boss",
                                    Kill = true,
                                    FriendlyPlayers = new[] { 1 },
                                    Difficulty = 100
                                }
                            },
                            MasterData = new LogInfo.ReportDataWrapper.ReportData.Report.Master
                            {
                                Actors = new[]
                                {
                                    new LogInfo.ReportDataWrapper.ReportData.Report.Master.Actor
                                    {
                                        Id = 1,
                                        Name = "TestPlayer",
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
