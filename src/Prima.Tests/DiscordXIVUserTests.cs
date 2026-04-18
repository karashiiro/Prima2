using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Prima.Game.FFXIV;

namespace Prima.Tests
{
    public class DiscordXIVUserTests
    {
        private const string TestCharacterName = "Karashiir Akhabila";
        private const string TestWorld = "Coeurl";
        private const ulong TestDiscordId = 128581209109430272;
        private const ulong TestLodestoneId = 20777669;
        private const string LodestoneApiUrl = "https://lodestone.universalis.app";

        private LodestoneClient lodestone;

        [SetUp]
        public void Setup()
        {
            this.lodestone = new LodestoneClient(LodestoneApiUrl);
        }

        [TearDown]
        public void TearDown()
        {
            this.lodestone?.Dispose();
        }

        [Test]
        public async Task CreateFromLodestoneSearch_KnownCharacter_ReturnsResult()
        {
            var (user, character) = await DiscordXIVUser.CreateFromLodestoneSearch(
                this.lodestone,
                TestCharacterName,
                TestWorld,
                TestDiscordId);

            Assert.That(user, Is.Not.Null, "Expected a non-null DiscordXIVUser");
            Assert.That(character, Is.Not.Null, "Expected a non-null LodestoneCharacter");

            Assert.That(user.DiscordId, Is.EqualTo(TestDiscordId));
            Assert.That(user.Name, Is.EqualTo(TestCharacterName));
            Assert.That(user.World, Is.EqualTo(TestWorld));
            Assert.That(user.LodestoneId, Is.Not.Empty);
            Assert.That(user.Avatar, Is.Not.Empty);

            Console.WriteLine($"Found character: {user.Name} on {user.World} (Lodestone ID: {user.LodestoneId})");
        }

        [Test]
        public async Task CreateFromLodestoneId_KnownCharacter_ReturnsResult()
        {
            var (user, character) = await DiscordXIVUser.CreateFromLodestoneId(
                this.lodestone,
                TestLodestoneId,
                TestDiscordId);

            Assert.That(user, Is.Not.Null, "Expected a non-null DiscordXIVUser");
            Assert.That(character, Is.Not.Null, "Expected a non-null LodestoneCharacter");

            Assert.That(user.DiscordId, Is.EqualTo(TestDiscordId));
            Assert.That(user.Name, Is.EqualTo(TestCharacterName));
            Assert.That(user.World, Is.EqualTo(TestWorld));
            Assert.That(user.LodestoneId, Is.EqualTo(TestLodestoneId.ToString()));
            Assert.That(user.Avatar, Is.Not.Empty);

            Console.WriteLine($"Found character: {user.Name} on {user.World}");
            Console.WriteLine($"Avatar: {user.Avatar}");
        }

        [Test]
        public async Task LodestoneClient_VerifyCharacter_FindsToken()
        {
            // The test character has "128581209109430272" in their bio
            var result = await LodestoneUtils.VerifyCharacter(
                this.lodestone,
                TestLodestoneId,
                "128581209109430272");

            Assert.That(result, Is.True, "Expected Discord ID to be found in character bio");
        }
    }
}
