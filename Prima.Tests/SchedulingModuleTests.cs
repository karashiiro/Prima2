using NUnit.Framework;
using System;

namespace Prima.Tests
{
    public class SchedulingModuleTests
    {
        [Test]
        public void GetDateTime_HandlesNoon()
        {
            var output = Util.GetDateTime("7/30 12:00PM");

            Assert.AreEqual(output.Hour, 12);
            Assert.AreEqual(output.Minute, 0);
            Assert.AreEqual(output.Second, 0);
        }

        [Test]
        public void GetDateTime_WorksAsExpected()
        {
            // Would be nice to TestCase these, but I can't stick a DateTime in an attribute
            var output = Util.GetDateTime("7/29 1:00PM");

            Assert.AreEqual(output.Month, 7);
            Assert.AreEqual(output.Day, 29);
            Assert.AreEqual(output.Hour, 13);
            Assert.AreEqual(output.Minute, 0);
            Assert.AreEqual(output.Second, 0);

            output = Util.GetDateTime("7/31/2020 1:00AM");

            Assert.AreEqual(output, new DateTime(2020, 7, 31, 1, 0 ,0));
        }
    }
}