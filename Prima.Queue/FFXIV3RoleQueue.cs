using Newtonsoft.Json;
using Prima.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Prima.Queue
{
    public class FFXIV3RoleQueue
    {
        [JsonProperty] private readonly IList<(ulong, DateTime, bool)> _dpsQueue;
        [JsonProperty] private readonly IList<(ulong, DateTime, bool)> _healerQueue;
        [JsonProperty] private readonly IList<(ulong, DateTime, bool)> _tankQueue;

        public FFXIV3RoleQueue()
        {
            _dpsQueue = new SynchronizedCollection<(ulong, DateTime, bool)>();
            _healerQueue = new SynchronizedCollection<(ulong, DateTime, bool)>();
            _tankQueue = new SynchronizedCollection<(ulong, DateTime, bool)>();
        }

        public bool Enqueue(ulong userId, FFXIVRole role)
        {
            var queue = GetQueue(role);

            if (queue.Any(tuple => tuple.Item1 == userId)) return false;
            queue.Add((userId, DateTime.UtcNow, false));
            return true;
        }

        public ulong? Dequeue(FFXIVRole role)
        {
            var queue = GetQueue(role);

            if (queue.Count == 0) return null;
            var (user, _, _) = queue[0];
            queue.RemoveAt(0);

            RemoveAll(user);

            return user;
        }

        public bool Remove(ulong userId, FFXIVRole role)
        {
            return role switch
            {
                FFXIVRole.DPS => _dpsQueue.Remove(tuple => tuple.Item1 == userId),
                FFXIVRole.Healer => _healerQueue.Remove(tuple => tuple.Item1 == userId),
                FFXIVRole.Tank => _tankQueue.Remove(tuple => tuple.Item1 == userId),
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
                .Select(tuple => tuple.Item1)
                .Distinct()
                .Count();
        }

        public int GetPosition(ulong userId, FFXIVRole role)
        {
            return role switch
            {
                FFXIVRole.DPS => _dpsQueue.IndexOf(tuple => tuple.Item1 == userId) + 1,
                FFXIVRole.Healer => _healerQueue.IndexOf(tuple => tuple.Item1 == userId) + 1,
                FFXIVRole.Tank => _tankQueue.IndexOf(tuple => tuple.Item1 == userId) + 1,
                FFXIVRole.None => 0,
                _ => throw new NotImplementedException(),
            };
        }

        public void Refresh(ulong uid)
        {
            var dpsSpot = _dpsQueue.FirstOrDefault(tuple => tuple.Item1 == uid);
            if (dpsSpot != default)
            {
                var index = GetPosition(uid, FFXIVRole.DPS) - 1;
                _dpsQueue.Insert(index, (uid, DateTime.UtcNow, false));
                _dpsQueue.RemoveAt(index + 1);
            }

            var healerSpot = _healerQueue.FirstOrDefault(tuple => tuple.Item1 == uid);
            if (healerSpot != default)
            {
                var index = GetPosition(uid, FFXIVRole.Healer) - 1;
                _healerQueue.Insert(index, (uid, DateTime.UtcNow, false));
                _healerQueue.RemoveAt(index + 1);
            }

            var tankSpot = _tankQueue.FirstOrDefault(tuple => tuple.Item1 == uid);
            if (tankSpot != default)
            {
                var index = GetPosition(uid, FFXIVRole.Tank) - 1;
                _tankQueue.Insert(index, (uid, DateTime.UtcNow, false));
                _tankQueue.RemoveAt(index + 1);
            }
        }

        public void Shove(ulong uid, FFXIVRole role)
        {
            var queue = GetQueue(role);

            Remove(uid, role);
            queue.Insert(0, (uid, DateTime.UtcNow, false));
        }

        private void SetNotified(ulong uid, DateTime queueTime, FFXIVRole role)
        {
            var queue = GetQueue(role);

            var index = GetPosition(uid, role) - 1;
            Remove(uid, role);
            queue.Insert(index, (uid, queueTime, true));
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
                    .Where(tuple => !tuple.Item3)
                    .Where(tuple => (DateTime.UtcNow - tuple.Item2).TotalSeconds > secondsBeforeNow - gracePeriod)
                    .ToList();
            foreach (var (uid, dateTime, _) in _almostTimedOut)
            {
                SetNotified(uid, dateTime, FFXIVRole.Tank);
            }
            return _almostTimedOut.Select(tuple => tuple.Item1);
        }

        private IEnumerable<ulong> QueryTimeout(FFXIVRole role, double secondsBeforeNow)
        {
            var queue = GetQueue(role);

            return queue.RemoveAll(tuple => (DateTime.UtcNow - tuple.Item2).TotalSeconds > secondsBeforeNow, overload: true)
                .Select(tuple => tuple.Item1);
        }

        private IList<(ulong, DateTime, bool)> GetQueue(FFXIVRole role) => role switch
        {
            FFXIVRole.DPS => _dpsQueue,
            FFXIVRole.Healer => _healerQueue,
            FFXIVRole.Tank => _tankQueue,
            FFXIVRole.None => new List<(ulong, DateTime, bool)>(),
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
