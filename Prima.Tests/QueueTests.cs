using NUnit.Framework;

namespace Prima.Tests
{
    public class QueueTests
    {
        [Test]
        [TestCase("1d3h1t", 1, 3, 1)]
        [TestCase("1d1h", 1, 1, 0)]
        [TestCase("1d", 1, 0, 0)]
        [TestCase("1d 4healer 2 tank", 1, 4, 2)]
        [TestCase("4h1t1d", 1, 4, 1)]
        [TestCase("11h11t11d", 11, 11, 11)]
        [TestCase("8h88t1d", 1, 8, 88)]
        [TestCase("8h88t1d Trinity Seeker progression", 1, 8, 88)]
        public void GetDesiredRoleCounts_WorksAsExpected(string input, int dExpected, int hExpected, int tExpected)
        {
            var (d, h, t) = QueueUtil.GetDesiredRoleCounts(input);
            Assert.That(d == dExpected);
            Assert.That(h == hExpected);
            Assert.That(t == tExpected);
        }
    }
}