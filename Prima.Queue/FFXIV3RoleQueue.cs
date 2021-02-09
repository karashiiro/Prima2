using Newtonsoft.Json;
using Prima.Extensions;
using System;
using System.Collections.Generic;
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

        public bool Enqueue(ulong userId, FFXIVRole role)
        {
            var queue = GetQueue(role);

            if (queue.Any(tuple => tuple.Id == userId)) return false;
            queue.Add(new QueueSlot(userId));
            return true;
        }

        public ulong? Dequeue(FFXIVRole role)
        {
            var queue = GetQueue(role);
            if (queue.Count == 0) return null;
            var (user, _, _) = queue[0];
            RemoveAll(user);
            return user;
        }

        public bool Remove(ulong userId, FFXIVRole role)
        {
            return role switch
            {
                FFXIVRole.DPS => _dpsQueue.Remove(tuple => tuple.Id == userId),
                FFXIVRole.Healer => _healerQueue.Remove(tuple => tuple.Id == userId),
                FFXIVRole.Tank => _tankQueue.Remove(tuple => tuple.Id == userId),
                FFXIVRole.None => false,
                _ => throw new NotImplementedException(),
            };
        }

        public void RemoveAll(ulong user)
        {
            Remove(user, FFXIVRole.DPS);
            Remove(user, FFXIVRole.Healer);
            Remove(user, FFXIVRole.Tank);
        }

        public int Count(FFXIVRole role)
        {
            return role switch
            {
                FFXIVRole.DPS => _dpsQueue.Count,
                FFXIVRole.Healer => _healerQueue.Count,
                FFXIVRole.Tank => _tankQueue.Count,
                FFXIVRole.None => 0,
                _ => throw new NotImplementedException(),
            };
        }

        public int CountDistinct()
        {
            return _dpsQueue
                .Concat(_healerQueue)
                .Concat(_tankQueue)
                .Select(s => s.Id)
                .Distinct()
                .Count();
        }

        public int GetPosition(ulong userId, FFXIVRole role)
        {
            return role switch
            {
                FFXIVRole.DPS => _dpsQueue.IndexOf(s => s.Id == userId) + 1,
                FFXIVRole.Healer => _healerQueue.IndexOf(s => s.Id == userId) + 1,
                FFXIVRole.Tank => _tankQueue.IndexOf(s => s.Id == userId) + 1,
                FFXIVRole.None => 0,
                _ => throw new NotImplementedException(),
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

        public void Shove(ulong uid, FFXIVRole role)
        {
            var queue = GetQueue(role);
            var initialCount = queue.Count;
            Remove(uid, role);
            queue.Insert(0, new QueueSlot(uid));
            queue.RemoveAll(s => s == null, overload: true);
            var finalCount = queue.Count;
            if (initialCount != finalCount)
            {
                Log.Warning("Queue size is inconsistent with expected result! Queue state may be corrupt.");
            }
        }

        public void Insert(ulong uid, int position, FFXIVRole role)
        {
            var queue = GetQueue(role);
            var initialCount = queue.Count;
            Remove(uid, role);

            try
            {
                queue.Insert(position, new QueueSlot(uid));
            }
            catch (IndexOutOfRangeException)
            {
                Log.Error("Tried to insert {User} in out of range queue position {Position}; inserting them at back instead.", uid, position);
                Enqueue(uid, role);
            }

            queue.RemoveAll(s => s == null, overload: true);
            var finalCount = queue.Count;
            if (initialCount + 1 != finalCount)
            {
                Log.Warning("Queue size is inconsistent with expected result! Queue state may be corrupt.");
            }
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
