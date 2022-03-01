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

        [Test]
        public void GetDateTime_HandlesNoon()
        {
            var (output, _) = ScheduleUtils.GetDateTime("7/30 12:00PM");
            
            Assert.AreEqual(12, output.Hour);
            Assert.AreEqual(0, output.Minute);
            Assert.AreEqual(0, output.Second);
        }

        [Test]
        [TestCase("7/29 1:00PM", 7, 29, 13, 0, 0)]
        [TestCase("7/29 3PM", 7, 29, 15, 0, 0)]
        public void GetDateTime_WorksAsExpected(string input, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
        {
            var (output, _) = ScheduleUtils.GetDateTime(input);

            Assert.AreEqual(expectedMonth, output.Month);
            Assert.AreEqual(expectedDay, output.Day);
            Assert.AreEqual(expectedHour, output.Hour);
            Assert.AreEqual(expectedMinute, output.Minute);
            Assert.AreEqual(expectedSecond, output.Second);
        }

        [Test]
        [TestCase("7/29/2020 1:00PM", 2020, 7, 29, 13, 0, 0)]
        public void GetDateTime_WorksAsExpectedWithYear(string input, int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
        {
            var (output, _) = ScheduleUtils.GetDateTime(input);
            Assert.AreEqual(new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond), output);
        }
    }
}