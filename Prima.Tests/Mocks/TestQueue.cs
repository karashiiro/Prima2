using System.Collections.Generic;
using System.Linq;
using Prima.Queue;

namespace Prima.Tests.Mocks
{
    public class TestQueue : FFXIV3RoleQueue
    {
        public void AddSlot(QueueSlot slot, FFXIVRole role)
        {
            var queue = GetQueue(role);
            queue.Add(slot);
        }

        public bool EnqueueAndConfirm(ulong userId, FFXIVRole role, string eventId)
        {
            var success = base.Enqueue(userId, role, eventId);
            GetAllSlots().First(s => s.Id == userId).Confirmed = true;
            return success;
        }

        public IEnumerable<QueueSlot> GetAllSlots()
        {
            return GetQueue(FFXIVRole.DPS)
                .Concat(GetQueue(FFXIVRole.Healer))
                .Concat(GetQueue(FFXIVRole.Tank));
        }

        public IEnumerable<ulong> TryQueryTimeout(FFXIVRole role, double secondsBeforeNow, bool includeEvents = false)
        {
            return QueryTimeout(role, secondsBeforeNow, includeEvents);
        }
    }
}