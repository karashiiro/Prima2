using NUnit.Framework;
using Prima.Queue;

namespace Prima.Tests
{
    public class QueueTests
    {
        [Test]
        [TestCase("1d1h1t", 1, 1, 1)]
        [TestCase("1d1h", 1, 1, 0)]
        [TestCase("1d", 1, 0, 0)]
        [TestCase("1d 1healer 1 tank", 1, 1, 1)]
        [TestCase("1h1t1d", 1, 1, 1)]
        [TestCase("11h11t11d", 11, 11, 11)]
        [TestCase("8h88t1d", 1, 8, 88)]
        public void GetDesiredRoleCounts_WorksAsExpected(string input, int dExpected, int hExpected, int tExpected)
        {
            var (d, h, t) = QueueUtil.GetDesiredRoleCounts(input);
            Assert.That(d == dExpected);
            Assert.That(h == hExpected);
            Assert.That(t == tExpected);
        }
    }
}