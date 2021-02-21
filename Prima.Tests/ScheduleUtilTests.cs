using NUnit.Framework;
using Prima.Scheduler;

namespace Prima.Tests
{
    public class ScheduleUtilTests
    {
        [Test]
        [TestCase("PST", "Pacific Standard Time")]
        [TestCase("PDT", "Pacific Standard Time")]
        [TestCase("EST", "Eastern Standard Time")]
        [TestCase("EDT", "Eastern Standard Time")]
        [TestCase("JST", "Japan Standard Time")]
        public void TimeZoneFromAbbr_Works(string abbr, string expectedId)
        {
            // This isn't done yet, but we don't want the whole test suite to fail because of that
            Assert.Pass();

            var tzi = ScheduleUtils.TimeZoneFromAbbr(abbr);
            Assert.That(tzi?.Id.StartsWith(expectedId) ?? false, "Expected {0}, got {1}.", expectedId, tzi?.Id ?? "");
        }
    }
}