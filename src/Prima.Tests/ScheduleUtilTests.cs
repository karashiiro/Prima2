using System;
using NUnit.Framework;
using Prima.Application.Scheduling;

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
            Assert.That(tzi.BaseUtcOffset.Hours == expectedOffset, $"Expected {expectedOffsetUtc}, got {tzi.BaseUtcOffset}.");
        }

        [TestCase("HT", -10, -10)]
        [TestCase("AKT", -9, -8)]
        [TestCase("PT", -8, -7)]
        [TestCase("MT", -7, -6)]
        [TestCase("CT", -6, -5)]
        [TestCase("ET", -5, -4)]
        public void TimeZoneFromAbbr_Works_2c(string abbr, int expectedOffset1, int expectedOffset2)
        {
            var tzi = ScheduleUtils.TimeZoneFromAbbr(abbr);
            var expectedOffset1Utc = TimeSpan.FromHours(expectedOffset1);
            var expectedOffset2Utc = TimeSpan.FromHours(expectedOffset2);
            Assert.That(tzi.BaseUtcOffset.Hours == expectedOffset1 || tzi.BaseUtcOffset.Hours == expectedOffset2,
                $"Expected {expectedOffset1Utc} or {expectedOffset2Utc}, got {tzi.BaseUtcOffset}.");
        }

        [Test]
        public void ParseTime_HandlesNoon()
        {
            var (output, _) = ScheduleUtils.ParseTime("7/30 12:00PM");
            
            Assert.That(12 == output.Hour);
            Assert.That(0 == output.Minute);
            Assert.That(0 == output.Second);
        }

        [Test]
        [TestCase("7/29 1:00PM", 7, 29, 13, 0, 0)]
        [TestCase("7/29 3PM", 7, 29, 15, 0, 0)]
        public void ParseTime_WorksAsExpected(string input, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
        {
            var (output, _) = ScheduleUtils.ParseTime(input);

            Assert.That(expectedMonth == output.Month);
            Assert.That(expectedDay == output.Day);
            Assert.That(expectedHour == output.Hour);
            Assert.That(expectedMinute == output.Minute);
            Assert.That(expectedSecond == output.Second);
        }

        [Test]
        [TestCase("7/29/2020 1:00PM", 2020, 7, 29, 13, 0, 0)]
        public void ParseTime_WorksAsExpectedWithYear(string input, int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
        {
            var (output, _) = ScheduleUtils.ParseTime(input);
            Assert.That(new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond), Is.EqualTo(output.DateTime));
        }

        [Test]
        [TestCase("7/29 1:00PM PST", -8, 7, 29, 13, 0, 0)]
        [TestCase("7/29 1:00PM pst", -8, 7, 29, 13, 0, 0)]
        [TestCase("7/29 1:00PM PDT", -7, 7, 29, 13, 0, 0)]
        [TestCase("7/29 1:00PM pdt", -7, 7, 29, 13, 0, 0)]
        [TestCase("7/29 1:00PM EST", -5, 7, 29, 13, 0, 0)]
        [TestCase("7/29 1:00PM est", -5, 7, 29, 13, 0, 0)]
        [TestCase("7/29 1:00PM EDT", -4, 7, 29, 13, 0, 0)]
        [TestCase("7/29 1:00PM edt", -4, 7, 29, 13, 0, 0)]
        [TestCase("1/21 10:00 AM PST", -8, 1, 21, 10, 0, 0)]
        public void ParseTime_WorksAsExpectedWithTimeZone(string input, int expectedOffset, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
        {
            var (output, tzi) = ScheduleUtils.ParseTime(input);

            Assert.That(expectedMonth == output.Month);
            Assert.That(expectedDay == output.Day);
            Assert.That(expectedHour == output.Hour);
            Assert.That(expectedMinute == output.Minute);
            Assert.That(expectedSecond == output.Second);

            var expectedOffsetUtc = TimeSpan.FromHours(expectedOffset);
            Assert.That(tzi?.BaseUtcOffset.Hours == expectedOffset, $"Expected {expectedOffsetUtc}, got {tzi?.BaseUtcOffset}.");
        }

        [Test]
        [TestCase("7/29 1:00PM PT", -8, -7, 7, 29, 13, 0, 0)]
        [TestCase("7/29 1:00PM pt", -8, -7, 7, 29, 13, 0, 0)]
        [TestCase("7/29 1:00PM ET", -5, -4, 7, 29, 13, 0, 0)]
        [TestCase("7/29 1:00PM et", -5, -4, 7, 29, 13, 0, 0)]
        public void ParseTime_WorksAsExpectedWithTimeZone_2c(string input, int expectedOffset1, int expectedOffset2, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
        {
            var (output, tzi) = ScheduleUtils.ParseTime(input);

            Assert.That(expectedMonth == output.Month);
            Assert.That(expectedDay == output.Day);
            Assert.That(expectedHour == output.Hour);
            Assert.That(expectedMinute == output.Minute);
            Assert.That(expectedSecond == output.Second);

            var expectedOffset1Utc = TimeSpan.FromHours(expectedOffset1);
            var expectedOffset2Utc = TimeSpan.FromHours(expectedOffset2);
            Assert.That(tzi?.BaseUtcOffset.Hours == expectedOffset1 || tzi?.BaseUtcOffset.Hours == expectedOffset2,
                $"Expected {expectedOffset1Utc} or {expectedOffset2Utc}, got {tzi?.BaseUtcOffset}.");
        }

        [Test]
        [TestCase("7/29/2020 1:00PM PST", -8, 2020, 7, 29, 13, 0, 0)]
        [TestCase("7/29/2020 1:00PM pst", -8, 2020, 7, 29, 13, 0, 0)]
        [TestCase("7/29/2020 1:00PM PDT", -7, 2020, 7, 29, 13, 0, 0)]
        [TestCase("7/29/2020 1:00PM pdt", -7, 2020, 7, 29, 13, 0, 0)]
        [TestCase("7/29/2020 1:00PM EST", -5, 2020, 7, 29, 13, 0, 0)]
        [TestCase("7/29/2020 1:00PM est", -5, 2020, 7, 29, 13, 0, 0)]
        [TestCase("7/29/2020 1:00PM EDT", -4, 2020, 7, 29, 13, 0, 0)]
        [TestCase("7/29/2020 1:00PM edt", -4, 2020, 7, 29, 13, 0, 0)]
        [TestCase("2/11/2024 10:00 AM PST", -8, 2024, 2, 11, 10, 0, 0)]
        public void ParseTime_WorksAsExpectedWithYearAndTimeZone(string input, int expectedOffset, int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
        {
            var (output, tzi) = ScheduleUtils.ParseTime(input);

            Assert.That(expectedYear == output.Year);
            Assert.That(expectedMonth == output.Month);
            Assert.That(expectedDay == output.Day);
            Assert.That(expectedHour == output.Hour);
            Assert.That(expectedMinute == output.Minute);
            Assert.That(expectedSecond == output.Second);

            var expectedOffsetUtc = TimeSpan.FromHours(expectedOffset);
            Assert.That(tzi?.BaseUtcOffset.Hours == expectedOffset, $"Expected {expectedOffsetUtc}, got {tzi?.BaseUtcOffset}.");
        }
    }
}