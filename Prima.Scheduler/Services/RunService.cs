using System;
using System.Collections.Generic;
using System.Linq;
using Prima.Scheduler.Models;

namespace Prima.Scheduler.Services
{
    public class RunService
    {
        private const long Threshold = 10800000;

        private readonly IList<ScheduledRun> _scheduledRuns;

        public RunService()
        {
            // TODO enumerate folder contents and load json files
            _scheduledRuns = new List<ScheduledRun>();
        }

        public void Schedule(ScheduledRun run)
        {
            _scheduledRuns.Add(run);
            // TODO serialize
        }

        public bool TryUnschedule(DateTime runTime, ulong leaderId)
            => _scheduledRuns.Remove(_scheduledRuns.FirstOrDefault(sr => sr.RunTime == runTime && sr.LeaderId == leaderId));

        public bool TooTightFor(ScheduledRun run)
            => _scheduledRuns.Any(sr => Math.Abs(sr.RunTime.ToBinary() - run.RunTime.ToBinary()) < Threshold);

        public IList<ScheduledRun> GetScheduledRuns() => _scheduledRuns;
    }
}
