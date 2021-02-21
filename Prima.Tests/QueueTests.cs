using NUnit.Framework;
using Prima.Queue;

namespace Prima.Tests
{
    public class QueueTests
    {
        const ulong userId = 435164236432553542;
        const string eventId = "483597092876052452";

        [Test]
        public void CountDistinct_Event_Works()
        {
            var queue = new FFXIV3RoleQueue();
            queue.Enqueue(userId, FFXIVRole.DPS, eventId);
            queue.Enqueue(userId, FFXIVRole.Healer, eventId);
            queue.Enqueue(39284729857983498, FFXIVRole.DPS, eventId);
            queue.Enqueue(39284729857983498, FFXIVRole.Tank, eventId);
            queue.Enqueue(11859824769135435, FFXIVRole.Healer, eventId);
            queue.Enqueue(23147289374928357, FFXIVRole.Healer, null);
            queue.Enqueue(12437598275983457, FFXIVRole.Tank, "");
            Assert.AreEqual(3, queue.CountDistinct(eventId));
        }

        [Test]
        public void CountDistinct_Normal_Works()
        {
            var queue = new FFXIV3RoleQueue();
            queue.Enqueue(userId, FFXIVRole.DPS, null);
            queue.Enqueue(userId, FFXIVRole.Healer, "");
            queue.Enqueue(39284729857983498, FFXIVRole.DPS, "");
            queue.Enqueue(39284729857983498, FFXIVRole.Tank, "");
            queue.Enqueue(11859824769135435, FFXIVRole.Healer, null);
            queue.Enqueue(23147289374928357, FFXIVRole.Healer, eventId);
            Assert.AreEqual(3, queue.CountDistinct(null));
        }

        [TestCase(FFXIVRole.DPS)]
        [TestCase(FFXIVRole.Healer)]
        [TestCase(FFXIVRole.Tank)]
        public void Dequeue_Event_PullsForEvent(FFXIVRole role)
        {
            var queue = new FFXIV3RoleQueue();
            var enqueueSuccess = queue.Enqueue(userId, role, eventId);
            Assert.IsTrue(enqueueSuccess);

            var outUId = queue.Dequeue(role, eventId);
            Assert.IsNotNull(outUId);
        }

        [TestCase(FFXIVRole.DPS)]
        [TestCase(FFXIVRole.Healer)]
        [TestCase(FFXIVRole.Tank)]
        public void Dequeue_Event_DoesNotPullForNormal(FFXIVRole role)
        {
            var queue = new FFXIV3RoleQueue();
            var enqueueSuccess = queue.Enqueue(userId, role, null);
            Assert.IsTrue(enqueueSuccess);

            var outUId = queue.Dequeue(role, eventId);
            Assert.IsNull(outUId);
        }

        [TestCase(FFXIVRole.DPS)]
        [TestCase(FFXIVRole.Healer)]
        [TestCase(FFXIVRole.Tank)]
        public void Dequeue_Normal_PullsForNormal(FFXIVRole role)
        {
            var queue = new FFXIV3RoleQueue();
            var enqueueSuccess = queue.Enqueue(userId, role, null);
            Assert.IsTrue(enqueueSuccess);

            var outUId = queue.Dequeue(role, null);
            Assert.IsNotNull(outUId);
        }

        [TestCase(FFXIVRole.DPS)]
        [TestCase(FFXIVRole.Healer)]
        [TestCase(FFXIVRole.Tank)]
        public void Dequeue_Normal_DoesNotPullForEvent(FFXIVRole role)
        {
            var queue = new FFXIV3RoleQueue();
            var enqueueSuccess = queue.Enqueue(userId, role, eventId);
            Assert.IsTrue(enqueueSuccess);

            var outUId = queue.Dequeue(role, null);
            Assert.IsNull(outUId);
        }

        [TestCase(FFXIVRole.DPS)]
        [TestCase(FFXIVRole.Healer)]
        [TestCase(FFXIVRole.Tank)]
        public void Queue_EventId_NotCountedNormally(FFXIVRole role)
        {
            var queue = new FFXIV3RoleQueue();
            var enqueueSuccess = queue.Enqueue(userId, role, eventId);
            Assert.IsTrue(enqueueSuccess);

            var pos1 = queue.GetPosition(userId, role, null);
            Assert.AreEqual(0, pos1);
            var pos2 = queue.GetPosition(userId, role, eventId);
            Assert.AreEqual(1, pos2);

            var count1 = queue.Count(role, null);
            Assert.AreEqual(0, count1);
            var count2 = queue.Count(role, eventId);
            Assert.AreEqual(1, count2);
        }

        [Test]
        public void Queue_NoneRole_FailsGracefully()
        {
            var queue = new FFXIV3RoleQueue();
            queue.Enqueue(userId, FFXIVRole.None, null);
            Assert.AreEqual(0, queue.GetPosition(userId, FFXIVRole.None, null));
            Assert.AreEqual(0, queue.GetPosition(userId, FFXIVRole.DPS, null));
            Assert.AreEqual(0, queue.GetPosition(userId, FFXIVRole.Healer, null));
            Assert.AreEqual(0, queue.GetPosition(userId, FFXIVRole.Tank, null));
        }

        [TestCase(FFXIVRole.DPS)]
        [TestCase(FFXIVRole.Healer)]
        [TestCase(FFXIVRole.Tank)]
        public void Queue_NoParameters_MaintainsState_NullEvent(FFXIVRole role)
        {
            var queue = new FFXIV3RoleQueue();
            var enqueueSuccess = queue.Enqueue(userId, role, null);
            Assert.IsTrue(enqueueSuccess);
            var outUId = queue.Dequeue(role, null);
            Assert.IsTrue(outUId.HasValue);
            Assert.AreEqual(userId, outUId.Value);
        }

        [TestCase(FFXIVRole.DPS)]
        [TestCase(FFXIVRole.Healer)]
        [TestCase(FFXIVRole.Tank)]
        public void Queue_NoParameters_MaintainsState_EmptyEvent(FFXIVRole role)
        {
            var queue = new FFXIV3RoleQueue();
            var enqueueSuccess = queue.Enqueue(userId, role, "");
            Assert.IsTrue(enqueueSuccess);
            var outUId = queue.Dequeue(role, "");
            Assert.IsTrue(outUId.HasValue);
            Assert.AreEqual(userId, outUId.Value);
        }
    }
}