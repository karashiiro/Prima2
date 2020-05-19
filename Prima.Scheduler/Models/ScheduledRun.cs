using System;
using static Prima.Scheduler.Resources.RunDisplayTypes;

namespace Prima.Scheduler.Models
{
    public class ScheduledRun
    {
        public ulong MessageId { get; set; }
        public ulong EmbedMessageId { get; set; }
        public ulong LeaderId { get; set; }
        public DateTime RunTime { get; set; }
        public string Description { get; set; }
        public RunDisplayType RunKind { get; set; }
    }
}
