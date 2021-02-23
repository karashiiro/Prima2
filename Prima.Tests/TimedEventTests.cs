using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Prima.Tests
{
    [TestFixture]
    public class TimedEventTests
    {
        [Test]
        public async Task GetResult_CanBeRunInParallel()
        {
            var count = 0;
            for (var i = 0; i < 10; i++)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var task1 = new TimedEvent(1, 0.05,
                    () => Task.FromResult(false)).GetResult();
                var task2 = new TimedEvent(1, 0.05,
                    () => Task.FromResult(false)).GetResult();
                await Task.WhenAll(task1, task2);
                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds < 1150)
                    count++;
            }

            Assert.That(count > 7);
        }

        [Test]
        public async Task GetResult_ReturnsFalseOnFailure()
        {
            var result = await new TimedEvent(1, 0.05,
                () => Task.FromResult(false)).GetResult();
            Assert.IsFalse(result);
        }

        [Test]
        public async Task GetResult_ReturnsTrueOnSuccess()
        {
            var result = await new TimedEvent(1, 0.05,
                () => Task.FromResult(true)).GetResult();
            Assert.IsTrue(result);
        }

        [Test]
        public async Task GetResult_DoesNotReturnEarly()
        {
            var once = false;
            await new TimedEvent(1, 0.05, () =>
            {
                // ReSharper disable once InvertIf
                if (!once)
                {
                    once = true;
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }).GetResult();
            Assert.IsTrue(once);
        }
    }
}