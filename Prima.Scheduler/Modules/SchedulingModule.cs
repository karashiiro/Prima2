using Discord;
using Discord.Commands;
using Prima.Attributes;
using Prima.Models;
using Prima.Resources;
using Prima.Scheduler.Services;
using Prima.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeZoneNames;
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
        [Description("Schedule an event. See the commands channel for more detailed information.")]
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

            if (parameters.IndexOf(":", StringComparison.Ordinal) == -1)
            {
                await ReplyAsync($"{Context.User.Mention}, please specify a time for your run in your command!");
                return;
            }

            var coolParameters = RegexSearches.Whitespace.Split(parameters);

            if (coolParameters.Length == 0)
            {
                await ReplyAsync($"{Context.User.Mention}, please provide parameters with that command.\n" +
                                 "A well-formed command would look something like:\n" +
                                 $"`{prefix}schedule OZ Tuesday 5:00PM | This is a fancy description!`");
                return;
            }

            IUserMessage message = Context.Message;
            var multiplier = 1;
            for (var i = 0; i < multiplier; i++)
            {
                if (i != 0)
                {
                    message = await ReplyAsync($"This message carries the RSVPs for run {i + 1} of this sequence.");
                }

                var @event = new ScheduledEvent
                {
                    Description = description,
                    LeaderId = Context.User.Id,
                    GuildId = Context.Guild.Id,
                    MessageId3 = message.Id,
                    SubscribedUsers = new List<string>(),
                };

                foreach (var coolParameter in coolParameters)
                {
                    if (RegexSearches.Multiplier.Match(coolParameter).Success)
                    {
                        multiplier = int.Parse(RegexSearches.NonNumbers.Replace(coolParameter, string.Empty));
                        if (multiplier > 12)
                        {
                            await ReplyAsync(
                                $"{Context.User.Mention}, for your own sake, you cannot schedule more than 36 hours-worth of runs at a time.");
                            return;
                        }
                    }

                    foreach (var runType in Enum.GetNames(typeof(RunDisplayTypeBA)))
                    {
                        if (string.Equals(coolParameter, runType, StringComparison.InvariantCultureIgnoreCase))
                        {
                            @event.RunKind = Enum.Parse<RunDisplayTypeBA>(runType, true);
                            break;
                        }
                    }
                }

                if (Context.Channel.Id == guildConfig.ScheduleInputChannel)
                {
                    if (!Enum.IsDefined(typeof(RunDisplayTypeBA), @event.RunKind))
                    {
                        await ReplyAsync(
                            $"{Context.User.Mention}, please specify a kind of run in your parameter list.\n" +
                            $"Run kinds include: `[{string.Join(' ', Enum.GetNames(typeof(RunDisplayTypeBA)))}]`");
                        return;
                    }
                }
                else
                {
                    @event.RunKindCastrum = RunDisplayTypeCastrum.LL;
                }

                DateTime runTime;
                try
                {
                    runTime = Util.GetDateTime(parameters).AddHours(3 * i);
                }
                catch (ArgumentOutOfRangeException)
                {
                    await ReplyAsync($"{Context.User.Mention}, that date or time is invalid.");
                    return;
                }

                if (!await RuntimeIsValid(runTime))
                    return;

                @event.RunTime = runTime.ToBinary();

                var tzi = TimeZoneInfo.FindSystemTimeZoneById(Util.PstIdString());
                var tzAbbrs = TZNames.GetAbbreviationsForTimeZone(tzi.Id, "en-US");
                var tzAbbr = tzi.IsDaylightSavingTime(DateTime.Now) ? tzAbbrs.Daylight : tzAbbrs.Standard;

                await Context.Channel.SendMessageAsync(
                    $"{Context.User.Mention} has just scheduled a run on {runTime.DayOfWeek} at {runTime.ToShortTimeString()} ({tzAbbr})!\n" +
                    $"React to the 📳 on their message to be notified 30 minutes before it begins!");
                await message.AddReactionAsync(new Emoji("📳"));

                var color = @event.RunKindCastrum == RunDisplayTypeCastrum.None ? RunDisplayTypes.GetColor(@event.RunKind) : RunDisplayTypes.GetColorCastrum();
                var leaderName = (Context.User as IGuildUser)?.Nickname ?? Context.User.Username;

                if (Context.Channel.Id == guildConfig.ScheduleInputChannel)
                {
                    var embed = new EmbedBuilder()
                        .WithTitle(
                            $"Run scheduled by {leaderName} on {runTime.DayOfWeek} at {runTime.ToShortTimeString()} ({tzAbbr}) " +
                            $"[{runTime.DayOfWeek}, {(Month) runTime.Month} {runTime.Day}]!")
                        .WithColor(new Color(color.RGB[0], color.RGB[1], color.RGB[2]))
                        .WithDescription(
                            "React to the :vibration_mode: on their message to be notified 30 minutes before it begins!\n\n" +
                            $"**{Context.User.Mention}'s full message: {message.GetJumpUrl()}**\n\n" +
                            $"{new string(@event.Description.Take(1650).ToArray())}{(@event.Description.Length > 1650 ? "..." : "")}\n\n" +
                            $"**Schedule Overview: <{guildConfig.BASpreadsheetLink}>**")
                        .WithFooter(footer => { footer.Text = "Localized time:"; })
                        .WithTimestamp(runTime.AddHours(tzi.BaseUtcOffset.Hours))
                        .Build();

                    var scheduleOutputChannel = Context.Guild.GetTextChannel(guildConfig.ScheduleOutputChannel);
                    var embedMessage = await scheduleOutputChannel.SendMessageAsync(embed: embed);

                    @event.EmbedMessageId = embedMessage.Id;

                    await Db.AddScheduledEvent(@event);
                    await Sheets.AddEvent(@event, guildConfig.BASpreadsheetId);
                }
                else if (Context.Channel.Id == guildConfig.CastrumScheduleInputChannel)
                {
                    var embed = new EmbedBuilder()
                        .WithTitle(
                            $"Run scheduled by {leaderName} on {runTime.DayOfWeek} at {runTime.ToShortTimeString()} ({tzAbbr}) " +
                            $"[{runTime.DayOfWeek}, {(Month)runTime.Month} {runTime.Day}]!")
                        .WithColor(new Color(color.RGB[0], color.RGB[1], color.RGB[2]))
                        .WithDescription(
                            "React to the :vibration_mode: on their message to be notified 30 minutes before it begins!\n\n" +
                            $"**{Context.User.Mention}'s full message: {message.GetJumpUrl()}**\n\n" +
                            $"{new string(@event.Description.Take(1650).ToArray())}{(@event.Description.Length > 1650 ? "..." : "")}\n\n" +
                            $"**Schedule Overview: <{guildConfig.CastrumSpreadsheetLink}>**")
                        .WithFooter(footer => { footer.Text = "Localized time:"; })
                        .WithTimestamp(runTime)
                        .Build();

                    var scheduleOutputChannel = Context.Guild.GetTextChannel(guildConfig.CastrumScheduleOutputChannel);
                    var embedMessage = await scheduleOutputChannel.SendMessageAsync(embed: embed);

                    @event.EmbedMessageId = embedMessage.Id;

                    await Db.AddScheduledEvent(@event);
                    await Sheets.AddEvent(@event, guildConfig.CastrumSpreadsheetId);
                }
            }
        }

        [Command("unschedule")]
        [Description("Unschedule an event.")]
        public async Task UnscheduleAsync([Remainder] string content)
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return;
            if (Context.Channel.Id == guildConfig.ScheduleInputChannel &&
                Context.Channel.Id == guildConfig.CastrumScheduleInputChannel) return;

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

            if (when < DateTime.Now)
            {
                await ReplyAsync($"{Context.User.Mention}, that time has already passed!");
                return;
            }

            ScheduledEvent result;
            try
            {
                result = await Db.TryRemoveScheduledEvent(when, Context.User.Id);
            }
            catch
            {
                var botMaster = Context.Client.GetUser(Db.Config.BotMaster);
                await ReplyAsync($"An error occurred. The run may or may not have been deleted, pinging {(botMaster != null ? botMaster.Mention : "undefined")}.");
                return;
            }
            if (result == null)
            {
                await ReplyAsync($"{Context.User.Mention}, you don't seem to have a run scheduled at that day and time!");
                return;
            }

            var scheduleOutputChannel = Context.Guild.GetTextChannel(result.RunKindCastrum == RunDisplayTypeCastrum.None ? guildConfig.ScheduleOutputChannel : guildConfig.CastrumScheduleOutputChannel);
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
                    await Task.Delay(1800000);
                    await embedMessage.DeleteAsync();
                });
            }

            await ReplyAsync($"{Context.User.Mention}, the run has been unscheduled.");

            await Sheets.RemoveEvent(result, result.RunKindCastrum == RunDisplayTypeCastrum.None ? guildConfig.BASpreadsheetId : guildConfig.CastrumSpreadsheetId);

            var leaderName = (Context.User as IGuildUser)?.Nickname ?? Context.User.Username;
            foreach (var uid in result.SubscribedUsers)
            {
                var user = Context.Guild.GetUser(ulong.Parse(uid));
                if (user == null)
                    continue;
                await user.SendMessageAsync($"The run for reacted to, scheduled by {leaderName} on {when.DayOfWeek} at {when.ToShortTimeString()}, has been cancelled.");
            }
        }

        [Command("reschedule")]
        [Description("Reschedule an event.")]
        public async Task RescheduleAsync([Remainder] string parameters)
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return;
            if (Context.Channel.Id == guildConfig.ScheduleInputChannel &&
                Context.Channel.Id == guildConfig.CastrumScheduleInputChannel) return;
            var prefix = guildConfig.Prefix == ' ' ? Db.Config.Prefix : guildConfig.Prefix;

            var splitIndex = parameters.IndexOf("|", StringComparison.Ordinal);
            if (splitIndex == -1)
            {
                await ReplyAsync($"{Context.User.Mention}, please provide parameters with that command.\n" +
                                 "A well-formed command would look something like:\n" +
                                 $"`{prefix}reschedule Tuesday 5:00PM | Wednesday 6:30PM`");
                return;
            }

            var currentRunTimeParameters = parameters.Substring(0, splitIndex).Trim();
            var newRunTimeParameters = parameters.Substring(splitIndex + 1).Trim();

            DateTime currentRunTime;
            try
            {
                currentRunTime = Util.GetDateTime(currentRunTimeParameters);
            }
            catch (ArgumentOutOfRangeException)
            {
                await ReplyAsync($"{Context.User.Mention}, the first time is invalid.");
                return;
            }
            if (currentRunTime.Minute >= 45)
            {
                currentRunTime = currentRunTime.AddMinutes(-currentRunTime.Minute);
                currentRunTime = currentRunTime.AddHours(1);
            }
            else if (currentRunTime.Minute >= 15)
            {
                currentRunTime = currentRunTime.AddMinutes(-currentRunTime.Minute + 30);
            }
            else
            {
                currentRunTime = currentRunTime.AddMinutes(-currentRunTime.Minute);
            }

            var @event = currentRunTime < DateTime.Now ? null : Db.Events.FirstOrDefault(run => run.RunTime == currentRunTime.ToBinary() && run.LeaderId == Context.User.Id);
            if (@event == null)
            {
                await ReplyAsync($"{Context.User.Mention}, you don't seem to have a run scheduled at that day and time (or that time has passed)!");
                return;
            }

            DateTime newRunTime;
            try
            {
                newRunTime = Util.GetDateTime(newRunTimeParameters);
            }
            catch (ArgumentOutOfRangeException)
            {
                await ReplyAsync($"{Context.User.Mention}, the second time is invalid.");
                return;
            }
            if (newRunTime.Minute >= 45)
            {
                newRunTime = newRunTime.AddMinutes(-newRunTime.Minute);
                newRunTime = newRunTime.AddHours(1);
            }
            else if (newRunTime.Minute >= 15)
            {
                newRunTime = newRunTime.AddMinutes(-newRunTime.Minute + 30);
            }
            else
            {
                newRunTime = newRunTime.AddMinutes(-newRunTime.Minute);
            }

            if (newRunTime == currentRunTime)
            {
                await ReplyAsync($"{Context.User.Mention}, you can't reschedule a run to the same time!");
                return;
            }

            if (newRunTime < DateTime.Now)
            {
                await ReplyAsync($"{Context.User.Mention}, you can't schedule a run in the past!");
                return;
            }

            if (newRunTime > DateTime.Now.AddDays(28))
            {
                await ReplyAsync($"{Context.User.Mention}, runs are limited to being scheduled within the next 28 days.\n" +
                                 "Please choose an earlier day to schedule your run.");
                return;
            }

            if (Db.Events
                .Where(sr => sr.MessageId3 != @event.MessageId3)
                .Any(sr => newRunTime > DateTime.Now &&
                                    Math.Abs((DateTime.FromBinary(sr.RunTime) - newRunTime).TotalMilliseconds) <
                                    Threshold))
            {
                await ReplyAsync($"{Context.User.Mention}, a run is already scheduled within 3 hours of that time! " +
                                 "Please check the schedule and try again.");
                return;
            }

            await Sheets.RemoveEvent(@event, @event.RunKindCastrum == RunDisplayTypeCastrum.None ? guildConfig.BASpreadsheetId : guildConfig.CastrumSpreadsheetId);

            @event.RunTime = newRunTime.ToBinary();
            await Db.UpdateScheduledEvent(@event);

            var tzi = TimeZoneInfo.FindSystemTimeZoneById(Util.PstIdString());
            var tzAbbrs = TZNames.GetAbbreviationsForTimeZone(tzi.Id, "en-US");
            var tzAbbr = tzi.IsDaylightSavingTime(DateTime.Now) ? tzAbbrs.Daylight : tzAbbrs.Standard;

            var leaderName = (Context.User as IGuildUser)?.Nickname ?? Context.User.Username;
            var embedChannel = Context.Guild.GetTextChannel(@event.RunKindCastrum == RunDisplayTypeCastrum.None ? guildConfig.ScheduleOutputChannel : guildConfig.CastrumScheduleOutputChannel);
            var embedMessage = await embedChannel.GetMessageAsync(@event.EmbedMessageId) as IUserMessage;
            // ReSharper disable once PossibleNullReferenceException
            var embed = embedMessage.Embeds.FirstOrDefault()?.ToEmbedBuilder()
                .WithTitle($"Run scheduled by {leaderName} on {newRunTime.DayOfWeek} at {newRunTime.ToShortTimeString()} ({tzAbbr}) " +
                           $"[{newRunTime.DayOfWeek}, {(Month)newRunTime.Month} {newRunTime.Day}]!")
                .WithTimestamp(newRunTime)
                .Build();
            await embedMessage.ModifyAsync(properties => properties.Embed = embed);

            await Sheets.AddEvent(@event, @event.RunKindCastrum == RunDisplayTypeCastrum.None ? guildConfig.BASpreadsheetId : guildConfig.CastrumSpreadsheetId);

            await ReplyAsync("Run rescheduled successfully.");
            foreach (var uid in @event.SubscribedUsers)
            {
                var user = Context.Guild.GetUser(ulong.Parse(uid));
                if (user == null)
                    continue;
                await user.SendMessageAsync($"The run for reacted to, scheduled by {leaderName} on {currentRunTime.DayOfWeek} at {currentRunTime.ToShortTimeString()}, has been rescheduled to {newRunTime.DayOfWeek} at {newRunTime.ToShortTimeString()}");
            }
        }

        [Command("rundst")]
        [Alias("rundistribution", "rundstr")]
        [Description("See the historical distribution of runs across the day.")]
        public Task GetRunDstAsync(params string[] args)
        {
            var runKind = (RunDisplayTypeBA)(-1);
            var color = new Color();
            if (args.Length != 0)
            {
                try
                {
                    runKind = (RunDisplayTypeBA)Enum.Parse(typeof(RunDisplayTypeBA), args[0], true);
                    var runKindColor = RunDisplayTypes.GetColor(runKind);
                    color = new Color(runKindColor.RGB[0], runKindColor.RGB[1], runKindColor.RGB[2]);
                }
                catch (ArgumentException) { /* Nothing to do */ }
            }

            var embed = new EmbedBuilder()
                .WithTitle($"Historical Scheduled Runs by Hour {(Enum.IsDefined(typeof(RunDisplayTypeBA), runKind) ? $"({runKind})" : string.Empty)}")
                .WithColor(Enum.IsDefined(typeof(RunDisplayTypeBA), runKind) ? color : Color.DarkTeal)
                .WithFooter("RSVP'd users may not be reflective of users who actually took part in a run.");

            var runsByHour = Db.Events
                .Where(@event => @event.Notified && !Enum.IsDefined(typeof(RunDisplayTypeBA), runKind) || @event.RunKind == runKind)
                .Select(@event =>
                {
                    var runTime = DateTime.FromBinary(@event.RunTime);
                    return new { Value = @event, Hour = runTime.Hour * 2 };
                })
                .GroupBy(kvp => kvp.Hour, kvp => kvp.Value)
                .OrderBy(bucket => bucket.Key);

            foreach (var runBucket in runsByHour)
            {
                var hour = runBucket.Key / 2;
                if (hour > 12) hour -= 12;
                var label = $"{hour}:00 {(runBucket.Key > 24 ? "PM" : "AM")}";
                embed = embed.AddField(label, $"{runBucket.Count()} runs (Average {Math.Round(runBucket.Aggregate(0, (i, @event) => i += @event.SubscribedUsers.Count) / (double)runBucket.Count(), 2)} users per run)");
            }

            return ReplyAsync(embed: embed.Build());
        }

        [Command("restoreevent")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RestoreEvent(params string[] args)
        {
            var messageId = ulong.Parse(args[0]);
            var embedId = ulong.Parse(args[1]);
            var notified = args.Length == 3;

            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return;
            var channel = Context.Guild.GetTextChannel(guildConfig.ScheduleInputChannel);

            var message = await channel.GetMessageAsync(messageId);
            if (message == null)
            {
                await ReplyAsync("x");
                return;
            }

            var splitIndex = message.Content.IndexOf("|", StringComparison.Ordinal);

            var parameters = message.Content.Substring(0, splitIndex).Trim();
            var description = message.Content.Substring(splitIndex + 1).Trim();

            var coolParameters = RegexSearches.Whitespace.Split(parameters);

            var reactors = await message.GetReactionUsersAsync(new Emoji("📳"), 100).FlattenAsync();
            var @event = new ScheduledEvent
            {
                Description = description,
                LeaderId = message.Author.Id,
                GuildId = Context.Guild.Id,
                MessageId3 = message.Id,
                EmbedMessageId = embedId,
                SubscribedUsers = reactors
                    .Where(user => user.Id != message.Author.Id && !user.IsBot)
                    .Select(user => user.Id.ToString())
                    .ToList(),
                Notified = true,
                Listed = true,
            };

            foreach (var coolParameter in coolParameters)
            {
                foreach (var runType in Enum.GetNames(typeof(RunDisplayTypeBA)))
                {
                    if (string.Equals(coolParameter, runType, StringComparison.InvariantCultureIgnoreCase))
                    {
                        @event.RunKind = Enum.Parse<RunDisplayTypeBA>(runType, true);
                        break;
                    }
                }
            }

            var runTime = Util.GetDateTime(parameters);

            @event.RunTime = runTime.ToBinary();

            await Db.AddScheduledEvent(@event);

            await ReplyAsync($"success, new count: {Db.Events.Count()}");
        }

        [RequireOwner]
        [Command("rebuildposts")]
        public Task RebuildPosts()
        {
            return ScheduleUtils.RebuildPosts(Db, Context.Client, Context.Guild.Id);
        }

        private async Task<bool> RuntimeIsValid(DateTime runTime)
        {
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
                return false;
            }

            if (runTime > DateTime.Now.AddDays(28))
            {
                await ReplyAsync($"{Context.User.Mention}, runs are limited to being scheduled within the next 28 days.\n" +
                                 "Please choose an earlier day to schedule your run.");
                return false;
            }

            if (Db.Events.Any(sr => runTime > DateTime.Now &&
                                    Math.Abs((DateTime.FromBinary(sr.RunTime) - runTime).TotalMilliseconds) <
                                    Threshold))
            {
                await ReplyAsync($"{Context.User.Mention}, a run is already scheduled within 3 hours of that time! " +
                                 "Please check the schedule and try again.");
                return false;
            }

            return true;
        }
    }
}
