using NUnit.Framework;
using Prima.DiscordNet.Extensions;
using Prima.DiscordNet.Services;
using System.Linq;

namespace Prima.Tests
{
    [TestFixture]
    public class TemplateProviderTests
    {
        private TemplateProvider _templates;

        [SetUp]
        public void Setup()
        {
            _templates = new TemplateProvider();
        }

        [Test]
        public void TestFile_Exists()
        {
            Assert.True(
                _templates.GetNames().Contains("test.md"),
                "Expected test.md, got [{0}]",
                string.Join(',', _templates.GetNames()));
        }

        [Test]
        public void TestFile_IsExecutable()
        {
            const string token = "cool token";
            var text = _templates.Execute("test.md", new
            {
                Token = token,
            }).Text;

            Assert.True(
                text.Contains($"Oh, wow, I have a {token}!"),
                "Expected to find \"Oh, wow, I have a {0}!\", got\n{1}",
                token,
                text);
        }

        [Test]
        public void DirectoryTree_Works()
        {
            Assert.True(
                _templates.GetNames().Contains("test/test.md"),
                "Expected test/test.md, got [{0}]",
                string.Join(',', _templates.GetNames()));

            const string token = "cool token";
            var text = _templates.Execute("test/test.md", new
            {
                Token = token,
            }).Text;

            Assert.True(
                text.Contains("This is another template."),
                "Expected to find \"This is another template.\", got\n{0}",
                text);
        }

        [Test]
        public void TestFile_ConvertsToEmbed()
        {
            const string token = "cool token";
            var rt = _templates.Execute("test.md", new
            {
                Token = token,
            });

            var embed = rt.ToEmbedBuilder().Build();
            Assert.AreEqual("This is a cool file.", embed.Title);
            Assert.AreEqual($"Oh, wow, I have a {token}!", embed.Description);
        }
    }
}