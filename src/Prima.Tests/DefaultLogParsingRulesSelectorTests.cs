using System;
using NUnit.Framework;
using Prima.Game.FFXIV.FFLogs.Rules;
using Prima.Models.FFLogs;

namespace Prima.Tests
{
    [TestFixture]
    public class DefaultLogParsingRulesSelectorTests
    {
        private DefaultLogParsingRulesSelector _rulesSelector;

        [SetUp]
        public void Setup()
        {
            _rulesSelector = new DefaultLogParsingRulesSelector();
        }

        [TestCase("Demon Tablet", typeof(ForkedTowerRules))]
        [TestCase("Dead Stars", typeof(ForkedTowerRules))]
        [TestCase("Marble Dragon", typeof(ForkedTowerRules))]
        [TestCase("Magitaur", typeof(ForkedTowerRules))]
        [TestCase("Trinity Seeker", typeof(DelubrumReginaeSavageRules))]
        [TestCase("Trinity Avowed", typeof(DelubrumReginaeSavageRules))]
        [TestCase("The Queen", typeof(DelubrumReginaeSavageRules))]
        [TestCase("Unknown Boss", null)]
        [TestCase("Random Encounter", null)]
        public void GetParsingRules_ReturnsExpectedRules(string encounterName, Type? expectedType)
        {
            var report = CreateTestReportWithEncounter(encounterName);
            if (expectedType != null)
            {
                var result = _rulesSelector.GetParsingRules(report);
                Assert.That(result, Is.InstanceOf(expectedType));
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => _rulesSelector.GetParsingRules(report));
            }
        }

        private static LogInfo.ReportDataWrapper.ReportData.Report CreateTestReportWithEncounter(string encounterName)
        {
            return new LogInfo.ReportDataWrapper.ReportData.Report
            {
                Fights = new[]
                {
                    new LogInfo.ReportDataWrapper.ReportData.Report.Fight
                    {
                        Name = encounterName,
                        Kill = true,
                        Difficulty = 100,
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
            };
        }
    }
}