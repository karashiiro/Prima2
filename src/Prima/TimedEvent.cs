using System;
using System.Threading.Tasks;

namespace Prima
{
    public class TimedEvent
    {
        private readonly double _timeoutSeconds;
        private readonly int _checkIntervalMs;
        private readonly Func<Task<bool>> _query;

        public TimedEvent(double timeoutSeconds, double checkIntervalSeconds, Func<Task<bool>> query)
        {
            _timeoutSeconds = timeoutSeconds;
            _checkIntervalMs = (int)(checkIntervalSeconds * 1000);
            _query = query;
        }

        public TimedEvent(double timeoutSeconds, double checkIntervalSeconds, Func<bool> query) : this(timeoutSeconds, checkIntervalSeconds,
            () => Task.FromResult(query())) { }

        public async Task<bool> GetResult()
        {
            var endTime = DateTime.UtcNow.AddSeconds(_timeoutSeconds);
            while (DateTime.UtcNow < endTime)
            {
                if (await _query())
                {
                    return true;
                }

                await Task.Delay(_checkIntervalMs);
            }

            return false;
        }
    }
}