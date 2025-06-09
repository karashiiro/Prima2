using System.Linq;
using NUnit.Framework;
using Prima.Game.FFXIV.FFLogs.Rules;
using Prima.Resources;

namespace Prima.Tests
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

        [TestCase("Trinity Seeker", "Trinity Seeker Progression")]
        [TestCase("The Queen's Guard", "Queen's Guard Progression")]
        [TestCase("Queen's Guard", "Queen's Guard Progression")]
        [TestCase("Trinity Avowed", "Trinity Avowed Progression")]
        [TestCase("The Queen", "The Queen Progression")]
        [TestCase("Unknown Boss", null)]
        [TestCase("Random Encounter", null)]
        public void GetProgressionRoleName_ReturnsExpectedRole(string encounterName, string expectedRole)
        {
            var result = _rules.GetProgressionRoleName(encounterName);
            Assert.That(result, Is.EqualTo(expectedRole));
        }

        [Test]
        public void GetKillRoleId_TrinitySeeker_ReturnsQueensGuard()
        {
            var result = _rules.GetKillRoleId("Trinity Seeker Progression");
            var expectedRoleId = DelubrumProgressionRoles.Roles
                .First(kvp => kvp.Value == "Queen's Guard Progression").Key;
            Assert.That(result, Is.EqualTo(expectedRoleId));
        }

        [Test]
        public void GetKillRoleId_QueensGuard_ReturnsTrinityAvowed()
        {
            var result = _rules.GetKillRoleId("Queen's Guard Progression");
            var expectedRoleId = DelubrumProgressionRoles.Roles
                .First(kvp => kvp.Value == "Trinity Avowed Progression").Key;
            Assert.That(result, Is.EqualTo(expectedRoleId));
        }

        [Test]
        public void GetKillRoleId_TrinityAvowed_ReturnsTheQueen()
        {
            var result = _rules.GetKillRoleId("Trinity Avowed Progression");
            var expectedRoleId = DelubrumProgressionRoles.Roles
                .First(kvp => kvp.Value == "The Queen Progression").Key;
            Assert.That(result, Is.EqualTo(expectedRoleId));
        }

        [Test]
        public void GetKillRoleId_TheQueen_ReturnsDRSCleared()
        {
            var result = _rules.GetKillRoleId("The Queen Progression");
            Assert.That(result, Is.EqualTo(DelubrumProgressionRoles.ClearedDelubrumSavage));
        }

        [Test]
        public void GetKillRoleId_InvalidRole_ReturnsZero()
        {
            var result = _rules.GetKillRoleId("Invalid Progression");
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void GetContingentRoleIds_QueenProgression_ReturnsAllProgressionRoles()
        {
            var queenRole = DelubrumProgressionRoles.Roles
                .First(kvp => kvp.Value == "The Queen Progression").Key;
            var contingentRoles = _rules.GetContingentRoleIds(queenRole).ToList();

            // Should return all progression roles when at The Queen
            var expectedRoles = DelubrumProgressionRoles.Roles
                .Where(kvp => kvp.Value.EndsWith("Progression") && kvp.Value != "debug delub role")
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var expectedRole in expectedRoles)
            {
                Assert.That(contingentRoles, Does.Contain(expectedRole));
            }
        }

        [Test]
        public void GetContingentRoleIds_TrinitySeekerProgression_ReturnsSelfOnly()
        {
            var trinitySeekerRole = DelubrumProgressionRoles.Roles
                .First(kvp => kvp.Value == "Trinity Seeker Progression").Key;
            var contingentRoles = _rules.GetContingentRoleIds(trinitySeekerRole).ToList();

            Assert.That(contingentRoles.Count, Is.EqualTo(1));
            Assert.That(contingentRoles[0], Is.EqualTo(trinitySeekerRole));
        }

        [Test]
        public void GetContingentRoleIds_InvalidRoleId_ReturnsEmpty()
        {
            var contingentRoles = _rules.GetContingentRoleIds(999999).ToList();
            Assert.That(contingentRoles, Is.Empty);
        }

        [TestCase(100, false, Description = "Normal mode encounters should not be processed")]
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