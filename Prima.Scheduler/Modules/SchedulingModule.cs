using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Apis.Calendar.v3.Data;
using Prima.Models;
using Prima.Scheduler.Services;
using Prima.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Prima.Scheduler.Resources.RunDisplayTypes;

namespace Prima.Scheduler.Modules
{
    /// <summary>
    /// Includes commands pertaining to scheduling things on the calendar.
    /// </summary>
    [Name("Scheduling")]
    public class SchedulingModule : ModuleBase<SocketCommandContext>
    {
        public DbService Db { get; set; }
        public SchedulingService Scheduler { get; set; }

        [Command("schedule")]
        public async Task ScheduleAsync(params string[] parameters) // Schedules a sink.
        {
            if (!Db.Guilds.Any(guild => guild.Id == Context.Guild.Id) ||
                Context.Channel.Id != Db.Guilds.Single(guild => guild.Id == Context.Guild.Id).ScheduleInputChannel) return;

            var config = Db.Guilds.Single(g => g.Id == Context.Guild.Id);

            if (parameters.Length < 3)
            {
                var prefix = config.Prefix == ' ' ? Db.Config.Prefix : config.Prefix;
                await ReplyAsync($"{Context.User.Mention}, please enter the scheduling command with the arguments <type>, <day>, and <time>, e.g. `{prefix}schedule oz sun 1:30PM`.");
                return;
            }

            // Parse arguments.
            if (!Enum.TryParse(parameters[0].ToUpper(), out RunDisplayType runDisplayType))
            {
                await ReplyAsync($"{parameters[0]} is not a valid run identifier. Valid identifiers include `{Enum.GetNames(typeof(RunDisplayType)).Aggregate((str1, str2) => str1 + " " + str2)}`.");
                return;
            }

            DateTime runDate;
            try
            {
                runDate = Util.GetDateTime(Context.Message.Content);
            }
            catch (ArgumentException)
            {
                await ReplyAsync("Please provide a valid MM/DD/YYYY or xxxxxday date in your command.");
                return;
            }

            string description = parameters.Length > 3 ? parameters.Skip(3).Aggregate((str1, str2) => str1 + " " + str2) : string.Empty;

            SocketTextChannel scheduleChannel;
            try
            {
                scheduleChannel = Context.Guild.GetChannel(config.ScheduleOutputChannel) as SocketTextChannel;
            }
            catch (InvalidOperationException)
            {
                await ReplyAsync("No output channel configured.");
                return;
            }

            string leaderTag = Context.User.ToString();
            SocketGuildUser member = Context.Guild.GetUser(Context.User.Id);

            string calendarDescriptionText = $"({runDisplayType}) {description} [{leaderTag}]";

            // Request the calendar and check if there's a run 3 hours before or after this one. If there is, error and return.
            IList<Event> events = await Scheduler.GetEvents(config.BACalendarId);
            if (events.Where(e => (runDate - e.Start.DateTime).Abs() < new TimeSpan(3, 0, 0)).Any())
            {
                await ReplyAsync("There's already a run scheduled within three hours of your requested time! To minimize competition for instances, scheduled runs must be at least three hours apart.");
                return;
            }

            // Create the calendar entry.
            var eventInfo = new Event
            {
                ColorId = GetColor(runDisplayType).Hex,
                Description = calendarDescriptionText,
                Start = new EventDateTime
                {
                    DateTime = runDate,
                },
            };
            await Scheduler.AddEvent(config.BACalendarId, eventInfo);

            // Create a file to store IDs of reactors.
            StreamWriter stream;
            int i = 0;
            while (File.Exists(Path.Combine(Environment.CurrentDirectory, "..", "schedules", i.ToString())))
            {
                i++;
            }
            stream = File.CreateText(Path.Combine(Environment.CurrentDirectory, "..", "schedules", i.ToString()));
            await stream.WriteLineAsync($"{runDate.ToBinary()},{Context.User.Id},{Context.Message.Id}");

            // Create the embed and post it.
            Embed messageEmbed = new EmbedBuilder()
                .WithTitle($"{member.Nickname ?? member.ToString()} has just scheduled a run on {runDate.DayOfWeek}, {runDate.Month} {runDate.Day} at {runDate.ToShortTimeString()}!")
                .WithDescription($"React to the 📳 on their message to be notified 30 minutes before it begins!\n\n" +
                    $"**{member.Nickname ?? member.ToString()}'s message: {Context.Message.GetJumpUrl()}**\n\n" +
                    $"{description.Take(1600).ToString()}\n\n" +
                    $"**Schedule Overview: https://calendar.google.com/calendar?cid={config.BACalendarId}**")
                .Build();
            await scheduleChannel.SendMessageAsync(embed: messageEmbed);

            // Respond to the command directly.
            await ReplyAsync($"{Context.User.Mention} has just scheduled a run on {runDate.DayOfWeek}, {runDate.Month} {runDate.Day} at {runDate.ToShortTimeString()}!\n" +
                $"React to the 📳 on their message to be notified 30 minutes before it begins!");
            await Context.Message.AddReactionAsync(new Emoji("📳"));
        }

        [Command("unschedule")]
        public async Task UnscheduleAsync([Remainder] string content)
        {
            DateTime runDate;
            try
            {
                runDate = Util.GetDateTime(content);
            }
            catch (ArgumentException)
            {
                await ReplyAsync("Please provide a valid MM/DD/YYYY or xxxxxday date in your command.");
                return;
            }

            DiscordGuildConfiguration guildConfig;
            try
            {
                guildConfig = Db.Guilds.Single(g => g.Id == Context.Guild.Id);
            }
            catch (InvalidOperationException)
            {
                await ReplyAsync("Please use this command in the guild you scheduled the run in.");
                return;
            }

            ulong scheduleEmbedId = 0;
            foreach (string filePath in Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "..", "schedules")))
            {
                using FileStream data = File.OpenRead(filePath);
                string[] fields = data.ToString().Split(',');
                if (fields[1] == Context.User.Id.ToString() && fields[0] == runDate.ToBinary().ToString())
                {
                    IList<Event> events = await Scheduler.GetEvents(guildConfig.BACalendarId);
                    if (events.Any(e => runDate == e.Start.DateTime))
                    {
                        scheduleEmbedId = ulong.Parse(fields[2]);
                        await Scheduler.DeleteEvent(guildConfig.BACalendarId, events.Single(e => runDate == e.Start.DateTime).Id);
                        data.Close();
                        File.Delete(filePath);
                    }
                    break;
                }
            }

            var outputChannel = Context.Client.GetChannel(guildConfig.ScheduleOutputChannel) as SocketTextChannel;
            try
            {
                await outputChannel.DeleteMessageAsync(scheduleEmbedId);
            }
            catch
            {
                scheduleEmbedId = 0;
            }
            
            if (scheduleEmbedId != 0)
            {
                await ReplyAsync("The run was unscheduled!");
            }
            else
            {
                await ReplyAsync($"No run matching that date and time was found. Parsed date: `{runDate.ToString()}`");
            }
        }
    }
}
