using System.Linq;
using NUnit.Framework;
using Prima.Resources;

namespace Prima.Tests
{
    [TestFixture]
    public class DelubProgRolesTests
    {
        [Test]
        public void QueenProg_ReturnsAll()
        {
            var role = DelubrumProgressionRoles.Roles
                .First(kvp => kvp.Value == "The Queen Progression").Key;
            var contingentRoles = DelubrumProgressionRoles.GetContingentRoles(role)
                .ToList();
            var contingentRolesExpected = DelubrumProgressionRoles.Roles
                .Where(kvp => kvp.Value != "The Queen Progression")
                .Where(kvp => kvp.Value != "debug delub role")
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var cr in contingentRolesExpected)
            {
                Assert.That(contingentRoles, Does.Contain(cr));
            }
        }

        [Test]
        public void TrinitySeekerProg_ReturnsSelf()
        {
            var role = DelubrumProgressionRoles.Roles
                .First(kvp => kvp.Value == "Trinity Seeker Progression").Key;
            var contingentRoles = DelubrumProgressionRoles.GetContingentRoles(role)
                .ToList();
            Assert.That(contingentRoles.Count == 1 && contingentRoles[0] == role);
        }
    }
}