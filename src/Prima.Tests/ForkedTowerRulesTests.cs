using System.Linq;
using NUnit.Framework;
using Prima.Game.FFXIV.FFLogs.Rules;

namespace Prima.Tests
{
    [TestFixture]
    public class ForkedTowerRulesTests
    {
        private ForkedTowerRules _rules;

        [SetUp]
        public void Setup()
        {
            _rules = new ForkedTowerRules();
        }

        [TestCase("Demon Tablet", "Demon Tablet Progression")]
        [TestCase("Dead Stars", "Dead Stars Progression")]
        [TestCase("Marble Dragon", "Marble Dragon Progression")]
        [TestCase("Magitaur", "Magitaur Progression")]
        [TestCase("Unknown Boss", null)]
        [TestCase("Random Encounter", null)]
        public void GetProgressionRoleName_ReturnsExpectedRole(string encounterName, string expectedRole)
        {
            var result = _rules.GetProgressionRoleName(encounterName);
            Assert.That(result, Is.EqualTo(expectedRole));
        }

        [Test]
        public void GetKillRoleId_DemonTablet_ReturnsDeadStars()
        {
            var result = _rules.GetKillRoleId("Demon Tablet Progression");
            var expectedRoleId = ForkedTowerRules.Roles
                .First(kvp => kvp.Value == "Dead Stars Progression").Key;
            Assert.That(result, Is.EqualTo(expectedRoleId));
        }

        [Test]
        public void GetKillRoleId_DeadStars_ReturnsMarbleDragon()
        {
            var result = _rules.GetKillRoleId("Dead Stars Progression");
            var expectedRoleId = ForkedTowerRules.Roles
                .First(kvp => kvp.Value == "Marble Dragon Progression").Key;
            Assert.That(result, Is.EqualTo(expectedRoleId));
        }

        [Test]
        public void GetKillRoleId_MarbleDragon_ReturnsMagitaur()
        {
            var result = _rules.GetKillRoleId("Marble Dragon Progression");
            var expectedRoleId = ForkedTowerRules.Roles
                .First(kvp => kvp.Value == "Magitaur Progression").Key;
            Assert.That(result, Is.EqualTo(expectedRoleId));
        }

        [Test]
        public void GetKillRoleId_Magitaur_ReturnsFTCleared()
        {
            var result = _rules.GetKillRoleId("Magitaur Progression");
            Assert.That(result, Is.EqualTo(_rules.FinalClearRoleId));
        }

        [Test]
        public void GetKillRoleId_InvalidRole_ReturnsZero()
        {
            var result = _rules.GetKillRoleId("Invalid Progression");
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void GetContingentRoleIds_MagitaurProgression_ReturnsAllProgressionRoles()
        {
            var magitaurRole = ForkedTowerRules.Roles
                .First(kvp => kvp.Value == "Magitaur Progression").Key;
            var contingentRoles = _rules.GetContingentRoleIds(magitaurRole).ToList();

            // Should return all progression roles when at Magitaur
            var expectedRoles = ForkedTowerRules.Roles
                .Where(kvp => kvp.Value.EndsWith("Progression"))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var expectedRole in expectedRoles)
            {
                Assert.That(contingentRoles, Does.Contain(expectedRole));
            }
        }

        [Test]
        public void GetContingentRoleIds_MagitaurKill_ReturnsAllProgressionRoles()
        {
            var contingentRoles = _rules.GetContingentRoleIds(_rules.FinalClearRoleId).ToList();

            // Should return all progression roles when cleared
            var expectedRoles = ForkedTowerRules.Roles
                .Where(kvp => kvp.Value.EndsWith("Progression"))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var expectedRole in expectedRoles)
            {
                Assert.That(contingentRoles, Does.Contain(expectedRole));
            }
        }

        [Test]
        public void GetContingentRoleIds_DemonTabletProgression_ReturnsSelfOnly()
        {
            var demonTabletRole = ForkedTowerRules.Roles
                .First(kvp => kvp.Value == "Demon Tablet Progression").Key;
            var contingentRoles = _rules.GetContingentRoleIds(demonTabletRole).ToList();

            Assert.That(contingentRoles.Count, Is.EqualTo(1));
            Assert.That(contingentRoles[0], Is.EqualTo(demonTabletRole));
        }

        [Test]
        public void GetContingentRoleIds_InvalidRoleId_ReturnsEmpty()
        {
            var contingentRoles = _rules.GetContingentRoleIds(999999).ToList();
            Assert.That(contingentRoles, Is.Empty);
        }

        [TestCase(100, true, Description = "Normal mode encounters should be processed")]
        [TestCase(101, true, Description = "Savage encounters should be processed")]
        [TestCase(null, true, Description = "Null difficulty should be processed")]
        [TestCase(0, true, Description = "Zero difficulty should be processed")]
        [TestCase(99, true, Description = "Any non-100 difficulty should be processed")]
        public void ShouldProcessEncounter_ReturnsExpectedResult(int? difficulty, bool expected)
        {
            var result = _rules.ShouldProcessEncounter(difficulty);
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}