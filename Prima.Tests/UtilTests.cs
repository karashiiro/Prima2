using System;
using NUnit.Framework;

namespace Prima.Tests
{
    [TestFixture]
    public class UtilTests
    {
        [Test]
        public void GetDateTime_HandlesNoon()
        {
            var output = Util.GetDateTime("7/30 12:00PM");

            Assert.AreEqual(12, output.Hour);
            Assert.AreEqual(0, output.Minute);
            Assert.AreEqual(0, output.Second);
        }

        [Test]
        [TestCase("7/29 1:00PM", 7, 29, 13, 0, 0)]
        public void GetDateTime_WorksAsExpected(string input, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
        {
            var output = Util.GetDateTime(input);

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
            var output = Util.GetDateTime(input);
            Assert.AreEqual(new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond), output);
        }
    }
}