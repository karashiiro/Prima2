using System;
using NUnit.Framework;
using Prima.Scheduler;

namespace Prima.Tests
{
    [TestFixture]
    public class ScheduleUtilTests
    {
        [Test]
        [TestCase("HST", -10)]
        [TestCase("AKST", -9)]
        [TestCase("AKDT", -8)]
        [TestCase("PST", -8)]
        [TestCase("PDT", -7)]
        [TestCase("MST", -7)]
        [TestCase("MDT", -6)]
        [TestCase("CST", -6)]
        [TestCase("CDT", -5)]
        [TestCase("EST", -5)]
        [TestCase("EDT", -4)]
        public void TimeZoneFromAbbr_Works(string abbr, int expectedOffset)
        {
            var tzi = ScheduleUtils.TimeZoneFromAbbr(abbr);
            var expectedOffsetUtc = TimeSpan.FromHours(expectedOffset);
            Assert.That(tzi?.BaseUtcOffset == expectedOffsetUtc, "Expected {0}, got {1}.", expectedOffsetUtc, tzi?.BaseUtcOffset);
        }
    }
}