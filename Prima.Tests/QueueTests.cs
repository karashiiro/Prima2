using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Prima.Queue;

namespace Prima.Tests
{
    public class QueueTests
    {
        const ulong userId = 435164236432553542;
        const string eventId = "483597092876052452";

        [TestCase(FFXIVRole.DPS)]
        [TestCase(FFXIVRole.Healer)]
        [TestCase(FFXIVRole.Tank)]
        public void Insert_Works_Event(FFXIVRole role)
        {
            var queue = new FFXIV3RoleQueue();
            queue.Enqueue(userId, role, eventId);
            queue.Insert(userId, 0, role);
            Assert.AreEqual(1, queue.GetPosition(userId, role, eventId));
        }

        [TestCase(FFXIVRole.DPS)]
        [TestCase(FFXIVRole.Healer)]
        [TestCase(FFXIVRole.Tank)]
        public void Insert_Works_NoEvent(FFXIVRole role)
        {
            var queue = new FFXIV3RoleQueue();
            queue.Enqueue(userId, role, "");
            queue.Insert(userId, 0, role);
            Assert.AreEqual(1, queue.GetPosition(userId, role, null));
        }

        [TestCase(FFXIVRole.DPS)]
        [TestCase(FFXIVRole.Healer)]
        [TestCase(FFXIVRole.Tank)]
        public void Shove_Works_Event(FFXIVRole role)
        {
            var queue = new FFXIV3RoleQueue();
            queue.Enqueue(userId, role, eventId);
            queue.Shove(userId, role);
            Assert.AreEqual(1, queue.GetPosition(userId, role, eventId));
        }

        [TestCase(FFXIVRole.DPS)]
        [TestCase(FFXIVRole.Healer)]
        [TestCase(FFXIVRole.Tank)]
        public void Shove_Works_NoEvent(FFXIVRole role)
        {
            var queue = new FFXIV3RoleQueue();
            queue.Enqueue(userId, role, "");
            queue.Shove(userId, role);
            Assert.AreEqual(1, queue.GetPosition(userId, role, null));
        }

        [Test]
        public async Task RefreshEvent_Works()
        {
            var rand = new Random(1234);
            var queue = new TestQueue();
            for (ulong i = 0; i < 1000; i++)
            {
                var nextRole = rand.Next(0, 3) switch
                {
                    0 => FFXIVRole.DPS,
                    1 => FFXIVRole.Healer,
                    2 => FFXIVRole.Tank,
                    _ => FFXIVRole.None,
                };
                queue.Enqueue(i, nextRole, eventId);
                await Task.Delay(rand.Next(1, 20));
            }
            queue.RefreshEvent(eventId);
            var timestamps = queue.GetAllSlots()
                .Select(slot => slot.QueueTime)
                .ToList();
            var firstTimestamp = timestamps.First();
            foreach (var timestamp in timestamps.Skip(1))
            {
                Assert.AreEqual(firstTimestamp, timestamp);
            }
        }

        [TestCase(FFXIVRole.DPS)]
        [TestCase(FFXIVRole.Healer)]
        [TestCase(FFXIVRole.Tank)]
        public async Task Refresh_Works_Event(FFXIVRole role)
        {
            var queue = new TestQueue();
            var enqueuedSlot = new QueueSlot(userId, eventId, new List<ulong>())
            {
                QueueTime = DateTime.UtcNow.AddHours(-4).AddMinutes(-45),
            };
            queue.AddSlot(enqueuedSlot, role);
            await Task.Delay(1000);
            var now = DateTime.UtcNow;
            queue.Refresh(userId);
            var dequeuedSlot = queue.GetAllSlots().First();
            Assert.AreEqual(now.Hour, dequeuedSlot.QueueTime.Hour);
            Assert.AreEqual(now.Minute, dequeuedSlot.QueueTime.Minute);
        }

        [TestCase(FFXIVRole.DPS)]
        [TestCase(FFXIVRole.Healer)]
        [TestCase(FFXIVRole.Tank)]
        public async Task Refresh_Works_NoEvent(FFXIVRole role)
        {
            var queue = new TestQueue();
            var enqueuedSlot = new QueueSlot(userId, "", new List<ulong>())
            {
                QueueTime = DateTime.UtcNow.AddHours(-4).AddMinutes(-45),
            };
            queue.AddSlot(enqueuedSlot, role);
            await Task.Delay(1000);
            var now = DateTime.UtcNow;
            queue.Refresh(userId);
            var dequeuedSlot = queue.GetAllSlots().First();
            Assert.AreEqual(now.Hour, dequeuedSlot.QueueTime.Hour);
            Assert.AreEqual(now.Minute, dequeuedSlot.QueueTime.Minute);
        }

        [TestCase(FFXIVRole.DPS)]
        [TestCase(FFXIVRole.Healer)]
        [TestCase(FFXIVRole.Tank)]
        public void SetEvent_Works(FFXIVRole role)
        {
            var queue = new FFXIV3RoleQueue();
            queue.Enqueue(userId, role, eventId);
            queue.SetEvent(userId, role, null);
            var userEventId = queue.GetEvent(userId, role);
            Assert.That(string.IsNullOrEmpty(userEventId));
        }

        [Test]
        public void GetEvents_Works_1()
        {
            var queue = new FFXIV3RoleQueue();
            queue.Enqueue(userId, FFXIVRole.DPS, eventId);
            queue.Enqueue(0, FFXIVRole.Tank, eventId);
            queue.Enqueue(1, FFXIVRole.Healer, eventId);
            queue.Enqueue(2, FFXIVRole.Healer, null);
            queue.Enqueue(3, FFXIVRole.Tank, "");

            var eventIds = queue.GetEvents().ToList();
            Assert.That(eventIds.Count == 1);
            Assert.That(eventIds[0] == eventId);
        }

        [Test]
        public void GetEvents_Works_2()
        {
            var queue = new FFXIV3RoleQueue();
            queue.Enqueue(userId, FFXIVRole.DPS, eventId);
            queue.Enqueue(0, FFXIVRole.Tank, "b");
            queue.Enqueue(1, FFXIVRole.Healer, "a");
            queue.Enqueue(2, FFXIVRole.Healer, null);
            queue.Enqueue(3, FFXIVRole.Tank, "");

            var eventIds = queue.GetEvents().ToList();
            Assert.IsNotNull(eventIds.FirstOrDefault(eId => eId == eventId));
            Assert.IsNotNull(eventIds.FirstOrDefault(eId => eId == "b"));
            Assert.IsNotNull(eventIds.FirstOrDefault(eId => eId == "a"));
        }

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