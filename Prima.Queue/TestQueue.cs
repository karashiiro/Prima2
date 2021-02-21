using System.Collections.Generic;
using System.Linq;

namespace Prima.Queue
{
    public class TestQueue : FFXIV3RoleQueue
    {
        public void AddSlot(QueueSlot slot, FFXIVRole role)
        {
            var queue = GetQueue(role);
            queue.Add(slot);
        }

        public IEnumerable<QueueSlot> GetAllSlots()
        {
            return GetQueue(FFXIVRole.DPS)
                .Concat(GetQueue(FFXIVRole.Healer))
                .Concat(GetQueue(FFXIVRole.Tank));
        }
    }
}