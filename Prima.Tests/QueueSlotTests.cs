using System.Linq;
using NUnit.Framework;
using Prima.Queue;
using Prima.Tests.Mocks;

namespace Prima.Tests
{
    [TestFixture]
    public class QueueSlotTests
    {
        [Test]
        public void EventId_FormatConversion_Works()
        {
            var oldSlot = new QueueSlot(0, "something")
            {
                EventIds = null,
            };
            var eventIds = oldSlot.EventIds.ToList();
            Assert.That(eventIds[0] == "something");
            oldSlot.EventIds = oldSlot.EventIds.Append("dangerous");
            eventIds = oldSlot.EventIds.ToList();
            Assert.That(eventIds[0] == "something" && eventIds[1] == "dangerous");
        }
    }
}