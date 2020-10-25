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
            switch (role)
            {
                case FFXIVRole.DPS:
                    if (_dpsQueue.Any(tuple => tuple.Item1 == userId)) return false;
                    _dpsQueue.Add((userId, DateTime.UtcNow, false));
                    return true;
                case FFXIVRole.Healer:
                    if (_healerQueue.Any(tuple => tuple.Item1 == userId)) return false;
                    _healerQueue.Add((userId, DateTime.UtcNow, false));
                    return true;
                case FFXIVRole.Tank:
                    if (_tankQueue.Any(tuple => tuple.Item1 == userId)) return false;
                    _tankQueue.Add((userId, DateTime.UtcNow, false));
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        public ulong? Dequeue(FFXIVRole role)
        {
            ulong user;
            switch (role)
            {
                case FFXIVRole.DPS:
                    if (_dpsQueue.Count == 0) return null;
                    (user, _, _) = _dpsQueue[0];
                    _dpsQueue.RemoveAt(0);
                    break;
                case FFXIVRole.Healer:
                    if (_healerQueue.Count == 0) return null;
                    (user, _, _) = _healerQueue[0];
                    _healerQueue.RemoveAt(0);
                    break;
                case FFXIVRole.Tank:
                    if (_tankQueue.Count == 0) return null;
                    (user, _, _) = _tankQueue[0];
                    _tankQueue.RemoveAt(0);
                    break;
                default:
                    throw new NotImplementedException();
            }

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
            switch (role)
            {
                case FFXIVRole.DPS:
                    Remove(uid, FFXIVRole.DPS);
                    _dpsQueue.Insert(0, (uid, DateTime.UtcNow, false));
                    break;
                case FFXIVRole.Healer:
                    Remove(uid, FFXIVRole.Healer);
                    _healerQueue.Insert(0, (uid, DateTime.UtcNow, false));
                    break;
                case FFXIVRole.Tank:
                    Remove(uid, FFXIVRole.Tank);
                    _tankQueue.Insert(0, (uid, DateTime.UtcNow, false));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void SetNotified(ulong uid, DateTime queueTime, FFXIVRole role)
        {
            switch (role)
            {
                case FFXIVRole.DPS:
                    Remove(uid, FFXIVRole.DPS);
                    _dpsQueue.Insert(0, (uid, queueTime, true));
                    break;
                case FFXIVRole.Healer:
                    Remove(uid, FFXIVRole.Healer);
                    _healerQueue.Insert(0, (uid, queueTime, true));
                    break;
                case FFXIVRole.Tank:
                    Remove(uid, FFXIVRole.Tank);
                    _tankQueue.Insert(0, (uid, queueTime, true));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public (IEnumerable<ulong>, IEnumerable<ulong>) Timeout(double secondsBeforeNow, double gracePeriod)
        {
            var dpsTimedOut = _dpsQueue.RemoveAll(tuple => (DateTime.UtcNow - tuple.Item2).TotalSeconds > secondsBeforeNow, overload: true)
                .Select(tuple => tuple.Item1);
            var healersTimedOut = _healerQueue.RemoveAll(tuple => (DateTime.UtcNow - tuple.Item2).TotalSeconds > secondsBeforeNow, overload: true)
                .Select(tuple => tuple.Item1);
            var tanksTimedOut = _tankQueue.RemoveAll(tuple => (DateTime.UtcNow - tuple.Item2).TotalSeconds > secondsBeforeNow, overload: true)
                .Select(tuple => tuple.Item1);

            var _dpsAlmostTimedOut =
                _dpsQueue
                    .Where(tuple => !tuple.Item3)
                    .Where(tuple => (DateTime.UtcNow - tuple.Item2).TotalSeconds > secondsBeforeNow - gracePeriod)
                    .ToList();
            foreach (var (uid, dateTime, _) in _dpsAlmostTimedOut)
            {
                SetNotified(uid, dateTime, FFXIVRole.DPS);
            }
            var dpsAlmostTimedOut = _dpsAlmostTimedOut.Select(tuple => tuple.Item1);

            var _healersAlmostTimedOut =
                _healerQueue
                    .Where(tuple => !tuple.Item3)
                    .Where(tuple => (DateTime.UtcNow - tuple.Item2).TotalSeconds > secondsBeforeNow - gracePeriod)
                    .ToList();
            foreach (var (uid, dateTime, _) in _healersAlmostTimedOut)
            {
                SetNotified(uid, dateTime, FFXIVRole.Healer);
            }
            var healersAlmostTimedOut = _healersAlmostTimedOut.Select(tuple => tuple.Item1);

            var _tanksAlmostTimedOut =
                _tankQueue
                    .Where(tuple => !tuple.Item3)
                    .Where(tuple => (DateTime.UtcNow - tuple.Item2).TotalSeconds > secondsBeforeNow - gracePeriod)
                    .ToList();
            foreach (var (uid, dateTime, _) in _tanksAlmostTimedOut)
            {
                SetNotified(uid, dateTime, FFXIVRole.Tank);
            }
            var tanksAlmostTimedOut = _tanksAlmostTimedOut.Select(tuple => tuple.Item1);


            var almostTimedOut = dpsAlmostTimedOut.Concat(healersAlmostTimedOut).Concat(tanksAlmostTimedOut);
            return (dpsTimedOut.Concat(healersTimedOut).Concat(tanksTimedOut).Distinct(), almostTimedOut.Distinct());
        }
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
