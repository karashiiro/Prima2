using System.Linq;
using NUnit.Framework;
using Prima.Game.FFXIV.FFLogs.Rules;

namespace Prima.Tests.Game.FFXIV.FFLogs.Rules
{
    [TestFixture]
    public class DelubrumReginaeSavageRulesTests
    {
        private DelubrumReginaeSavageRules _rules;

        [SetUp]
        public void Setup()
        {
            _rules = new DelubrumReginaeSavageRules();
        }

        [Test]
        public void FinalClearRoleName_ReturnsDRSCleared()
        {
            Assert.AreEqual("DRS Cleared", _rules.FinalClearRoleName);
        }

        [TestCase("Trinity Seeker", "Trinity Seeker Progression")]
        [TestCase("The Queen's Guard", "Queen's Guard Progression")]
        [TestCase("Queen's Guard", "Queen's Guard Progression")]
        [TestCase("Trinity Avowed", "Trinity Avowed Progression")]
        [TestCase("Stygimoloch Lord", "Stygimoloch Lord Progression")]
        [TestCase("The Queen", "The Queen Progression")]
        [TestCase("Unknown Boss", null)]
        public void GetProgressionRoleName_ReturnsCorrectMapping(string encounterName, string expectedRole)
        {
            var result = _rules.GetProgressionRoleName(encounterName);
            Assert.AreEqual(expectedRole, result);
        }

        [TestCase(100, false)] // Normal mode
        [TestCase(101, true)]  // Savage mode
        [TestCase(null, true)] // No difficulty specified
        public void ShouldProcessEncounter_ReturnsCorrectValue(int? difficulty, bool expected)
        {
            var result = _rules.ShouldProcessEncounter(difficulty);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetContingentRoles_ReturnsExpectedRoles()
        {
            var result = _rules.GetContingentRoles("Trinity Seeker Progression").ToList();
            
            Assert.IsNotEmpty(result);
            Assert.Contains("Trinity Seeker Progression", result);
        }
    }
}
