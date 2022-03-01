using NUnit.Framework;
using Prima.Scheduler;

namespace Prima.Tests
{
    [TestFixture]
    public class ScheduleUtilTests
    {
        [Test]
        [TestCase("HST", "Hawaiian Standard Time")]
        [TestCase("AKST", "Alaskan Standard Time")]
        [TestCase("AKDT", "Alaskan Standard Time")]
        [TestCase("PST", "Pacific Standard Time")]
        [TestCase("PDT", "Pacific Standard Time")]
        [TestCase("MST", "Mountain Standard Time")]
        [TestCase("MDT", "Mountain Standard Time")]
        [TestCase("CST", "Central Standard Time")]
        [TestCase("CDT", "Central Standard Time")]
        [TestCase("EST", "Eastern Standard Time")]
        [TestCase("EDT", "Eastern Standard Time")]
        public void TimeZoneFromAbbr_Works(string abbr, string expectedId)
        {
            var tzi = ScheduleUtils.TimeZoneFromAbbr(abbr);
            Assert.That(tzi?.Id.StartsWith(expectedId) ?? false, "Expected {0}, got {1}.", expectedId, tzi?.Id ?? "");
        }
    }
}