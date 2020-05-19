using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Prima.Models;
using Prima.Scheduler.Services;
using Prima.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Prima.Scheduler.Modules
{
    /// <summary>
    /// Includes commands pertaining to scheduling things on the calendar.
    /// </summary>
    [Name("Scheduling")]
    public class SchedulingModule : ModuleBase<SocketCommandContext>
    {
        public DbService Db { get; set; }
        public SheetsService Scheduler { get; set; }

        [Command("schedule")]
        public async Task ScheduleAsync([Remainder] string content) // Schedules a sink.
        {
        }

        [Command("unschedule")]
        public async Task UnscheduleAsync([Remainder] string content)
        {
        }
    }
}
