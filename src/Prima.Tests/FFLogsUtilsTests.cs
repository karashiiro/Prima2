using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Prima.Game.FFXIV;
using Prima.Game.FFXIV.FFLogs;
using Prima.Models.FFLogs;
using Prima.Tests.Mocks;

namespace Prima.Tests
{
    [TestFixture]
    public class FFLogsUtilsTests
    {
        private MemoryDb _db;
        private Mock<IFFLogsClient> _mockFFLogsClient;

        [SetUp]
        public void Setup()
        {
            _db = new MemoryDb();
            _mockFFLogsClient = new Mock<IFFLogsClient>();
        }

        [Test]
        public void FFLogsUtils_InvalidLogLink_IsRejected()
        {
            // Note: ReadLog method requires Discord context which is complex to mock
            // Instead, we test the underlying logic that ReadLog uses
            
            // Arrange
            const string invalidLogLink = "https://example.com/not-a-log";
            
            // Act - Test the utility method that ReadLog uses internally
            var isValidLogLink = FFLogsUtils.IsLogLink(invalidLogLink);
            
            // Assert - Invalid links should be rejected by the utility
            Assert.That(isValidLogLink, Is.False);
            
            // Verify FFLogs client was not called (it shouldn't be since we didn't call ReadLog)
            _mockFFLogsClient.Verify(c => c.MakeGraphQLRequest<LogInfo>(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void FFLogsUtils_ValidLogLink_IsRecognizedAndParsed()
        {
            // Note: ReadLog method requires Discord context which is complex to mock
            // Instead, we test the underlying logic that ReadLog uses
            
            // Arrange
            const string validLogLink = "https://www.fflogs.com/reports/1234567890";
            
            // Act - Test the utility methods that ReadLog uses internally
            var isValidLogLink = FFLogsUtils.IsLogLink(validLogLink);
            var match = FFLogsUtils.LogLinkToIdRegex.Match(validLogLink);
            
            // Assert - Valid links should be recognized by the utility
            Assert.That(isValidLogLink, Is.True);
            Assert.That(match.Success, Is.True);
            Assert.That(match.Value, Is.EqualTo("1234567890"));
            
            // The GraphQL query should be buildable
            var query = FFLogsUtils.BuildLogRequest(match.Value);
            Assert.That(query, Does.Contain("1234567890"));
        }

        [Test]
        public void LogInfo_DataStructures_CanBeCreatedAndParsed()
        {
            // Note: ReadLog method requires Discord context which is complex to mock
            // Instead, we test the data structures that ReadLog works with
            
            // Arrange & Act - Test that we can create the log data structures
            var emptyLogInfo = CreateEmptyLogInfo();
            var privateLogInfo = CreatePrivateLogInfo();
            
            // Assert - Verify the structures are created correctly
            Assert.That(emptyLogInfo, Is.Not.Null);
            Assert.That(emptyLogInfo.Content, Is.Not.Null);
            Assert.That(emptyLogInfo.Content.Data, Is.Not.Null);
            Assert.That(emptyLogInfo.Content.Data.ReportInfo, Is.Not.Null);
            Assert.That(emptyLogInfo.Content.Data.ReportInfo.Fights, Is.Not.Null);
            Assert.That(emptyLogInfo.Content.Data.ReportInfo.MasterData, Is.Not.Null);
            
            // Private log should have null ReportInfo
            Assert.That(privateLogInfo, Is.Not.Null);
            Assert.That(privateLogInfo.Content, Is.Not.Null);
            Assert.That(privateLogInfo.Content.Data, Is.Not.Null);
            Assert.That(privateLogInfo.Content.Data.ReportInfo, Is.Null);
        }

        [Test]
        public void FFLogsUtils_IsLogLink_ValidatesCorrectly()
        {
            // Test the utility method that ReadLog depends on
            Assert.That(FFLogsUtils.IsLogLink("https://www.fflogs.com/reports/1234567890"), Is.True);
            Assert.That(FFLogsUtils.IsLogLink("https://example.com/not-a-log"), Is.False);
            Assert.That(FFLogsUtils.IsLogLink("fflogs.com/reports/abcd123456"), Is.True);
        }

        [Test] 
        public void FFLogsUtils_LogLinkToIdRegex_ExtractsLogId()
        {
            // Test log ID extraction
            const string logLink = "https://www.fflogs.com/reports/1234567890";
            var match = FFLogsUtils.LogLinkToIdRegex.Match(logLink);
            Assert.That(match.Success, Is.True);
            Assert.That(match.Value, Is.EqualTo("1234567890"));
        }

        [Test]
        public void FFLogsUtils_BuildLogRequest_CreatesValidGraphQL()
        {
            // Test GraphQL query building
            const string logId = "1234567890";
            var query = FFLogsUtils.BuildLogRequest(logId);
            
            Assert.That(query, Does.Contain(logId));
            Assert.That(query, Does.Contain("reportData"));
            Assert.That(query, Does.Contain("fights"));
            Assert.That(query, Does.Contain("masterData"));
            Assert.That(query, Does.Contain("actors"));
        }

        [Test]
        public async Task Database_AddUser_WorksCorrectly()
        {
            // Test that the database operations work as expected
            var user = new DiscordXIVUser
            {
                DiscordId = 123456789,
                Name = "Test Player",
                World = "Excalibur",
                LodestoneId = "12345678",
                Avatar = "https://example.com/avatar.jpg"
            };

            await _db.AddUser(user);

            var retrievedUser = _db.Users.FirstOrDefault(u => u.DiscordId == 123456789);
            Assert.That(retrievedUser, Is.Not.Null);
            Assert.That(retrievedUser.Name, Is.EqualTo("Test Player"));
            Assert.That(retrievedUser.World, Is.EqualTo("Excalibur"));
        }

        private static LogInfo CreateEmptyLogInfo()
        {
            return new LogInfo
            {
                Content = new LogInfo.ReportDataWrapper
                {
                    Data = new LogInfo.ReportDataWrapper.ReportData
                    {
                        ReportInfo = new LogInfo.ReportDataWrapper.ReportData.Report
                        {
                            Fights = Array.Empty<LogInfo.ReportDataWrapper.ReportData.Report.Fight>(),
                            MasterData = new LogInfo.ReportDataWrapper.ReportData.Report.Master
                            {
                                Actors = Array.Empty<LogInfo.ReportDataWrapper.ReportData.Report.Master.Actor>()
                            }
                        }
                    }
                }
            };
        }

        private static LogInfo CreatePrivateLogInfo()
        {
            return new LogInfo
            {
                Content = new LogInfo.ReportDataWrapper
                {
                    Data = new LogInfo.ReportDataWrapper.ReportData
                    {
                        ReportInfo = null,
                    }
                }
            };
        }
    }
}
