using System.Linq;
using NUnit.Framework;
using Prima.Game.FFXIV.FFLogs.Rules;

namespace Prima.Tests.Game.FFXIV.FFLogs.Rules
{
    [TestFixture]
    public class BaldesionArsenalRulesTests
    {
        private BaldesionArsenalRules _rules;

        [SetUp]
        public void Setup()
        {
            _rules = new BaldesionArsenalRules();
        }

        [Test]
        public void FinalClearRoleName_ReturnsNull()
        {
            Assert.IsNull(_rules.FinalClearRoleName);
        }

        [TestCase("Art", "Art Progression")]
        [TestCase("Owain", "Owain Progression")]
        [TestCase("Raiden", "Raiden Progression")]
        [TestCase("Absolute Virtue", "AV Progression")]
        [TestCase("Ozma", "Ozma Progression")]
        [TestCase("Unknown Boss", null)]
        public void GetProgressionRoleName_ReturnsCorrectMapping(string encounterName, string expectedRole)
        {
            var result = _rules.GetProgressionRoleName(encounterName);
            Assert.AreEqual(expectedRole, result);
        }

        [TestCase("Art Progression", "Art Clear")]
        [TestCase("Owain Progression", "Owain Clear")]
        [TestCase("Raiden Progression", "Raiden Clear")]
        [TestCase("AV Progression", "AV Clear")]
        [TestCase("Ozma Progression", "Ozma Clear")]
        [TestCase("Unknown Progression", null)]
        public void GetClearRoleName_ReturnsCorrectMapping(string progressionRole, string expectedClearRole)
        {
            var result = _rules.GetClearRoleName(progressionRole);
            Assert.AreEqual(expectedClearRole, result);
        }

        [Test]
        public void ShouldProcessEncounter_AlwaysReturnsTrue()
        {
            Assert.IsTrue(_rules.ShouldProcessEncounter(100));
            Assert.IsTrue(_rules.ShouldProcessEncounter(null));
            Assert.IsTrue(_rules.ShouldProcessEncounter(0));
        }

        [Test]
        public void GetContingentRoles_ReturnsProgressionRoleAndParticipant()
        {
            var result = _rules.GetContingentRoles("Art Progression").ToList();
            
            Assert.AreEqual(2, result.Count);
            Assert.Contains("Art Progression", result);
            Assert.Contains("BA Participant", result);
        }
    }
}
