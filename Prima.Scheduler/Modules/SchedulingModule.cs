using Discord;
using Discord.Commands;
using Prima.Models;
using Prima.Resources;
using Prima.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prima.Scheduler.Services;
using Serilog;
using Color = Discord.Color;

namespace Prima.Scheduler.Modules
{
    /// <summary>
    /// Includes commands pertaining to scheduling things on the calendar.
    /// </summary>
    [Name("Scheduling")]
    [RequireContext(ContextType.Guild)]
    public class SchedulingModule : ModuleBase<SocketCommandContext>
    {
        private const long Threshold = 10800000;

        public DbService Db { get; set; }
        public SpreadsheetService Sheets { get; set; }

        [Command("schedule")]
        public async Task ScheduleAsync([Remainder] string content) // Schedules a sink.
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return;
            if (Context.Channel.Id != guildConfig.ScheduleInputChannel) return;
            var prefix = guildConfig.Prefix == ' ' ? Db.Config.Prefix : guildConfig.Prefix;

            var splitIndex = content.IndexOf("|", StringComparison.Ordinal);
            if (splitIndex == -1)
            {
                await ReplyAsync($"{Context.User.Mention}, please provide parameters with that command.\n" +
                                 "A well-formed command would look something like:\n" +
                                 $"`{prefix}schedule OZ Tuesday 5:00PM | This is a fancy description!`");
                return;
            }

            var parameters = content.Substring(0, splitIndex).Trim();
            var description = content.Substring(splitIndex + 1).Trim();

            var coolParameters = RegexSearches.Whitespace.Split(parameters);

            DateTime runTime;
            try
            {
                runTime = Util.GetDateTime(parameters);
            }
            catch (ArgumentOutOfRangeException)
            {
                await ReplyAsync($"{Context.User.Mention}, that time is invalid.");
                return;
            }
            var @event = new ScheduledEvent
            {
                Description = description,
                LeaderId = Context.User.Id,
                GuildId = Context.Guild.Id,
                MessageId = Context.Message.Id,
                SubscribedUsers = new List<ulong>(),
            };

            if (runTime.Minute >= 45)
            {
                runTime = runTime.AddMinutes(-runTime.Minute);
                runTime = runTime.AddHours(1);
            }
            else if (runTime.Minute >= 15)
            {
                runTime = runTime.AddMinutes(-runTime.Minute + 30);
            }
            else
            {
                runTime = runTime.AddMinutes(-runTime.Minute);
            }

            if (runTime < DateTime.Now)
            {
                await ReplyAsync($"{Context.User.Mention}, you can't schedule a run in the past!");
                return;
            }

            if (Db.Events.Any(sr => runTime > DateTime.Now &&
                                    Math.Abs((DateTime.FromBinary(sr.RunTime) - runTime).TotalMilliseconds) <
                                    Threshold))
            {
                await ReplyAsync($"{Context.User.Mention}, a run is already scheduled within 3 hours of that time! " +
                                 "Please check the schedule and try again.");
                return;
            }

            @event.RunTime = runTime.ToBinary();

            if (coolParameters.Length == 0)
            {
                await ReplyAsync($"{Context.User.Mention}, please provide parameters with that command.\n" +
                                 "A well-formed command would look something like:\n" +
                                 $"`{prefix}schedule OZ Tuesday 5:00PM | This is a fancy description!`");
                return;
            }
            foreach (var coolParameter in coolParameters)
            {
                foreach (var runType in Enum.GetNames(typeof(RunDisplayType)))
                {
                    if (string.Equals(coolParameter, runType, StringComparison.InvariantCultureIgnoreCase))
                    {
                        @event.RunKind = Enum.Parse<RunDisplayType>(runType, true);
                        break;
                    }
                }
            }
            if (!Enum.IsDefined(typeof(RunDisplayType), @event.RunKind))
            {
                await ReplyAsync($"{Context.User.Mention}, please specify a kind of run in your parameter list.\n" +
                                 $"Run kinds include: `[{string.Join(' ', Enum.GetNames(typeof(RunDisplayType)))}]`");
                return;
            }

            await Context.Channel.SendMessageAsync($"{Context.User.Mention} has just scheduled a run on {runTime.DayOfWeek} at {runTime.ToShortTimeString()} (PDT)!\n" +
                                                   $"React to the 📳 on their message to be notified 30 minutes before it begins!");
            await Context.Message.AddReactionAsync(new Emoji("📳"));

            var color = RunDisplayTypes.GetColor(@event.RunKind);
            var leaderName = (Context.User as IGuildUser)?.Nickname ?? Context.User.Username;
            var embed = new EmbedBuilder()
                .WithTitle($"Run scheduled by {leaderName} on {runTime.DayOfWeek} at {runTime.ToShortTimeString()} (PDT) " +
                           $"[{runTime.DayOfWeek}, {(Month)runTime.Month} {runTime.Day}]!")
                .WithColor(new Color(color.RGB[0], color.RGB[1], color.RGB[2]))
                .WithDescription("React to the :vibration_mode: on their message to be notified 30 minutes before it begins!\n\n" +
                                 $"**{Context.User.Mention}'s full message: {Context.Message.GetJumpUrl()}**\n\n" +
                                 $"{new string(@event.Description.Take(1850).ToArray())}{(@event.Description.Length > 1850 ? "..." : "")}\n\n" +
                                 $"**Schedule Overview: <{guildConfig.BASpreadsheetLink}>**")
                .Build();

            var scheduleOutputChannel = Context.Guild.GetTextChannel(guildConfig.ScheduleOutputChannel);
            var embedMessage = await scheduleOutputChannel.SendMessageAsync(embed: embed);

            @event.EmbedMessageId = embedMessage.Id;

            await Db.AddScheduledEvent(@event);

            await Sheets.AddEvent(@event, guildConfig.BASpreadsheetId);
        }

        [Command("unschedule")]
        public async Task UnscheduleAsync([Remainder] string content)
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return;
            if (Context.Channel.Id != guildConfig.ScheduleInputChannel) return;

            DateTime when;
            try
            {
                when = Util.GetDateTime(content);
            }
            catch (ArgumentOutOfRangeException)
            {
                await ReplyAsync($"{Context.User.Mention}, that time is invalid.");
                return;
            }
            if (when.Minute >= 45)
            {
                when = when.AddMinutes(-when.Minute);
                when = when.AddHours(1);
            }
            else if (when.Minute >= 15)
            {
                when = when.AddMinutes(-when.Minute + 30);
            }
            else
            {
                when = when.AddMinutes(-when.Minute);
            }

            var result = when < DateTime.Now ? null : await Db.TryRemoveScheduledEvent(when, Context.User.Id);
            if (result == null)
            {
                await ReplyAsync("You don't seem to have a run scheduled at that day and time (or that time has passed)!");
                return;
            }

            var scheduleOutputChannel = Context.Guild.GetTextChannel(guildConfig.ScheduleOutputChannel);
            if (await scheduleOutputChannel.GetMessageAsync(result.EmbedMessageId) is IUserMessage embedMessage)
            {
                var embed = embedMessage.Embeds.First();
                var cancelledEmbed = new EmbedBuilder()
                    .WithTitle(embed.Title)
                    // ReSharper disable once PossibleInvalidOperationException
                    .WithColor(embed.Color.Value)
                    .WithDescription("❌ Cancelled")
                    .Build();
                await embedMessage.ModifyAsync(properties => properties.Embed = cancelledEmbed);
                _ = Task.Run(async () =>
                {
                    await Task.Delay(9000000);
                    await embedMessage.DeleteAsync();
                });
            }

            await ReplyAsync($"{Context.User.Mention}, the run has been unscheduled.");

            await Sheets.RemoveEvent(result, guildConfig.BASpreadsheetId);
        }
    }
}
