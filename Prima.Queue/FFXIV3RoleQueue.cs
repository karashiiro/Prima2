using Newtonsoft.Json;
using Prima.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Serilog;

namespace Prima.Queue
{
    public class FFXIV3RoleQueue
    {
        [JsonProperty] protected readonly IList<QueueSlot> _dpsQueue;
        [JsonProperty] protected readonly IList<QueueSlot> _healerQueue;
        [JsonProperty] protected readonly IList<QueueSlot> _tankQueue;

        public FFXIV3RoleQueue()
        {
            _dpsQueue = new SynchronizedCollection<QueueSlot>();
            _healerQueue = new SynchronizedCollection<QueueSlot>();
            _tankQueue = new SynchronizedCollection<QueueSlot>();
        }

        public bool Enqueue(ulong userId, FFXIVRole role, string eventId)
        {
            var queue = GetQueue(role);

            if (queue.Any(tuple => tuple.Id == userId)) return false;
            queue.Add(new QueueSlot(userId, eventId ?? ""));
            return true;
        }

        public ulong? Dequeue(FFXIVRole role, string eventId)
        {
            var queue = GetQueue(role);
            if (queue.Count == 0) return null;

            QueueSlot slot;
            if (string.IsNullOrEmpty(eventId))
                slot = queue.FirstOrDefault(s => !s.RoleIds.Any() && string.IsNullOrEmpty(s.EventId));
            else
                slot = queue.FirstOrDefault(s => !s.RoleIds.Any() && s.EventId == eventId);
            
            if (slot != null)
            {
                RemoveAll(slot.Id);
                return slot.Id;
            }
            else
            {
                return null;
            }
        }

        public bool Remove(ulong userId, FFXIVRole role)
        {
            return GetQueue(role)
                .Remove(s => s.Id == userId);
        }

        public void RemoveAll(ulong user)
        {
            Remove(user, FFXIVRole.DPS);
            Remove(user, FFXIVRole.Healer);
            Remove(user, FFXIVRole.Tank);
        }

        public IEnumerable<string> GetEvents()
        {
            return _dpsQueue
                .Concat(_healerQueue)
                .Concat(_tankQueue)
                .Select(slot => slot.EventId)
                .Where(eventId => !string.IsNullOrEmpty(eventId))
                .Distinct();
        }

        public int Count(FFXIVRole role, string eventId)
        {
            return GetQueue(role)
                .Where(s => !s.RoleIds.Any())
                .Count(EventValid(eventId));
        }

        public int CountDistinct(string eventId)
        {
            return _dpsQueue
                .Concat(_healerQueue)
                .Concat(_tankQueue)
                .Where(EventValid(eventId))
                .Where(s => !s.RoleIds.Any())
                .Select(s => s.Id)
                .Distinct()
                .Count();
        }

        public int GetPosition(ulong userId, FFXIVRole role, string eventId)
        {
            return GetQueue(role)
                .Where(s => !s.RoleIds.Any())
                .Where(EventValid(eventId))
                .ToList()
                .IndexOf(s => s.Id == userId) + 1;
        }

        public string GetEvent(ulong userId, FFXIVRole role)
        {
            return GetQueue(role)
                .FirstOrDefault(s => s.Id == userId)
                ?.EventId;
        }

        public void SetEvent(ulong userId, FFXIVRole role, string eventId)
        {
            var slot = GetQueue(role)
                .FirstOrDefault(s => s.Id == userId);
            if (slot != null)
            {
                slot.EventId = eventId;
            }
        }

        protected static Func<QueueSlot, bool> EventValid(string eventId)
        {
            return s =>
            {
                if (string.IsNullOrEmpty(eventId)) return string.IsNullOrEmpty(s.EventId);
                return s.EventId == eventId;
            };
        }

        public void Refresh(ulong uid)
        {
            var dpsSpot = _dpsQueue.FirstOrDefault(tuple => tuple.Id == uid);
            if (dpsSpot != null)
            {
                dpsSpot.QueueTime = DateTime.UtcNow;
                dpsSpot.ExpirationNotified = false;
            }

            var healerSpot = _healerQueue.FirstOrDefault(tuple => tuple.Id == uid);
            if (healerSpot != null)
            {
                healerSpot.QueueTime = DateTime.UtcNow;
                healerSpot.ExpirationNotified = false;
            }

            var tankSpot = _tankQueue.FirstOrDefault(tuple => tuple.Id == uid);
            if (tankSpot != null)
            {
                tankSpot.QueueTime = DateTime.UtcNow;
                tankSpot.ExpirationNotified = false;
            }
        }

        public void RefreshEvent(string eventId)
        {
            var now = DateTime.UtcNow;
            var allQueues = GetQueue(FFXIVRole.DPS)
                .Concat(GetQueue(FFXIVRole.Healer))
                .Concat(GetQueue(FFXIVRole.Tank))
                .Where(EventValid(eventId))
                .ToList();
            foreach (var slot in allQueues)
            {
                slot.QueueTime = now;
                slot.ExpirationNotified = false;
            }
        }

        public void Shove(ulong uid, FFXIVRole role)
        {
            Insert(uid, 0, role);
        }

        public void Insert(ulong uid, int position, FFXIVRole role)
        {
            var queue = GetQueue(role);

            var slot = queue.FirstOrDefault(s => s.Id == uid);
            if (slot != null)
            {
                slot.ExpirationNotified = false;
                slot.QueueTime = DateTime.UtcNow;
            }

            Remove(uid, role);

            try
            {
                queue.Insert(position, slot ?? new QueueSlot(uid));
            }
            catch (ArgumentOutOfRangeException)
            {
                Log.Error("Tried to insert {User} in out of range queue position {Position}; inserting them at back instead.", uid, position);
                Enqueue(uid, role, slot?.EventId ?? "");
            }

            queue.RemoveAll(s => s == null, overload: true);
        }

        public (IEnumerable<ulong>, IEnumerable<ulong>) Timeout(double secondsBeforeNow, double gracePeriod)
        {
            var dpsTimedOut = QueryTimeout(FFXIVRole.DPS, secondsBeforeNow);
            var healersTimedOut = QueryTimeout(FFXIVRole.Healer, secondsBeforeNow);
            var tanksTimedOut = QueryTimeout(FFXIVRole.Tank, secondsBeforeNow);

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (gracePeriod == 0)
            {
                return (dpsTimedOut.Concat(healersTimedOut).Concat(tanksTimedOut).Distinct(), null);
            }

            var dpsAlmostTimedOut = GetAlmostTimedOut(FFXIVRole.DPS, secondsBeforeNow, gracePeriod);
            var healersAlmostTimedOut = GetAlmostTimedOut(FFXIVRole.Healer, secondsBeforeNow, gracePeriod);
            var tanksAlmostTimedOut = GetAlmostTimedOut(FFXIVRole.Tank, secondsBeforeNow, gracePeriod);

            var almostTimedOut = dpsAlmostTimedOut.Concat(healersAlmostTimedOut).Concat(tanksAlmostTimedOut);
            return (dpsTimedOut.Concat(healersTimedOut).Concat(tanksTimedOut).Distinct(), almostTimedOut.Distinct());
        }

        private IEnumerable<ulong> GetAlmostTimedOut(FFXIVRole role, double secondsBeforeNow, double gracePeriod)
        {
            var queue = GetQueue(role);

            var _almostTimedOut =
                queue
                    .Where(s => !s.ExpirationNotified)
                    .Where(s => (DateTime.UtcNow - s.QueueTime).TotalSeconds > secondsBeforeNow - gracePeriod)
                    .ToList();
            foreach (var slot in _almostTimedOut)
                slot.ExpirationNotified = true;
            return _almostTimedOut.Select(tuple => tuple.Id);
        }

        private IEnumerable<ulong> QueryTimeout(FFXIVRole role, double secondsBeforeNow)
        {
            var queue = GetQueue(role);

            return queue.RemoveAll(s => (DateTime.UtcNow - s.QueueTime).TotalSeconds > secondsBeforeNow, overload: true)
                .Select(s => s.Id);
        }

        protected IList<QueueSlot> GetQueue(FFXIVRole role) => role switch
        {
            FFXIVRole.DPS => _dpsQueue,
            FFXIVRole.Healer => _healerQueue,
            FFXIVRole.Tank => _tankQueue,
            FFXIVRole.None => new List<QueueSlot>(),
            _ => throw new NotImplementedException(),
        };
    }

    [Flags]
    public enum FFXIVRole
    {
        None = 0,
        DPS = 1,
        Healer = 2,
        Tank = 4,
    }
}
