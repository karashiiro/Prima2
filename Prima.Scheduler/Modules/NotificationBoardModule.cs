using Discord;
using Discord.Commands;
using Prima.Attributes;
using Prima.Resources;
using Prima.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Prima.Scheduler.GoogleApis.Calendar;
using Prima.Scheduler.GoogleApis.Services;
using Serilog;
using TimeZoneNames;
using Color = Discord.Color;

namespace Prima.Scheduler.Modules
{
    [Name("NotificationBoard")]
    [RequireContext(ContextType.Guild)]
    public class NotificationBoardModule : ModuleBase<SocketCommandContext>
    {
        public CalendarApi Calendar { get; set; }
        public DbService Db { get; set; }

        [Command("announce", RunMode = RunMode.Async)]
        [Description("Announce an event. Usage: `~announce Time | Description`")]
        public async Task Announce([Remainder]string args)
        {
            var outputChannel = GetOutputChannel();
            if (outputChannel == null) return;

            var prefix = Db.Config.Prefix;

            var splitIndex = args.IndexOf("|", StringComparison.Ordinal);
            if (splitIndex == -1)
            {
                await ReplyAsync($"{Context.User.Mention}, please provide parameters with that command.\n" +
                                 "A well-formed command would look something like:\n" +
                                 $"`{prefix}announce 5:00PM | This is a fancy description!`");
                return;
            }

            var parameters = args.Substring(0, splitIndex).Trim();
            var description = args.Substring(splitIndex + 1).Trim();
            var trimmedDescription = description.Substring(0, Math.Min(1800, description.Length));
            if (trimmedDescription.Length != description.Length)
            {
                trimmedDescription += "...";
            }

            if (parameters.IndexOf(":", StringComparison.Ordinal) == -1)
            {
                await ReplyAsync($"{Context.User.Mention}, please specify a time for your run in your command!");
                return;
            }

            var time = Util.GetDateTime(parameters);
            if (time < DateTime.Now)
            {
                await ReplyAsync("You cannot announce an event in the past!");
                return;
            }

            var tzi = TimeZoneInfo.FindSystemTimeZoneById(Util.PstIdString());
            var tzAbbrs = TZNames.GetAbbreviationsForTimeZone(tzi.Id, "en-US");
            var tzAbbr = tzi.IsDaylightSavingTime(DateTime.Now) ? tzAbbrs.Daylight : tzAbbrs.Standard;

            var eventLink =
#if DEBUG
            await Calendar.PostEvent("drs", new MiniEvent
#else
            await Calendar.PostEvent(GetCalendarCode(outputChannel.Id), new MiniEvent
#endif
            {
                Title = Context.User.ToString(),
                Description = description,
                StartTime = XmlConvert.ToString(time.AddHours(-tzi.BaseUtcOffset.Hours), XmlDateTimeSerializationMode.Utc),
            });

            var color = RunDisplayTypes.GetColorCastrum();
            var embed = await outputChannel.SendMessageAsync(embed: new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithIconUrl(Context.User.GetAvatarUrl())
                    .WithName(Context.User.ToString()))
                .WithColor(new Color(color.RGB[0], color.RGB[1], color.RGB[2]))
                .WithTimestamp(time.AddHours(-tzi.BaseUtcOffset.Hours))
                .WithTitle($"Event scheduled by {Context.User} on {time.DayOfWeek} at {time.ToShortTimeString()} ({tzAbbr})!")
                .WithDescription(trimmedDescription + $"\n\n[Copy to Google Calendar]({eventLink})")
                .Build());
            await embed.AddReactionAsync(new Emoji("📳"));

            await ReplyAsync($"Event announced! Announcement posted in <#{outputChannel.Id}>. React to the announcement in " +
                             $"<#{outputChannel.Id}> with :vibration_mode: to be notified before the event begins.");
            await SortEmbeds(outputChannel);
        }

        private async Task<MiniEvent> FindEvent(string calendarClass, string title, DateTime startTime)
        {
            var events = await Calendar.GetEvents(calendarClass);
            return events.FirstOrDefault(e =>
            {
                var eventStartTime = XmlConvert.ToDateTime(e.StartTime, XmlDateTimeSerializationMode.Utc);
                return e.Title == title && eventStartTime == startTime;
            });
        }

        [Command("sortembeds", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task SortEmbedsCommand(ulong id)
        {
            var channel = Context.Guild.GetTextChannel(id);
            await SortEmbeds(channel);
            await ReplyAsync("Done!");
        }

        private async Task SortEmbeds(IMessageChannel channel)
        {
            var progress = await ReplyAsync("Sorting announcements...");
            using var typing = Context.Channel.EnterTypingState();

            var embeds = new List<IEmbed>();

            await foreach (var page in channel.GetMessagesAsync())
            {
                foreach (var message in page)
                {
                    if (message.Embeds.All(e => e.Type != EmbedType.Rich)) continue;
                    var embed = message.Embeds.First(e => e.Type == EmbedType.Rich);

                    if (!embed.Timestamp.HasValue) continue;

                    await message.DeleteAsync();
                    if (embed.Timestamp.Value < DateTimeOffset.Now) continue;

                    embeds.Add(embed);
                }
            }

            // ReSharper disable PossibleInvalidOperationException
            embeds.Sort((a, b) => (int)(b.Timestamp.Value.ToUnixTimeSeconds() - a.Timestamp.Value.ToUnixTimeSeconds()));
            // ReSharper enable PossibleInvalidOperationException

            foreach (var embed in embeds)
            {
                await channel.SendMessageAsync(embed: embed.ToEmbedBuilder().Build());
            }

            await progress.DeleteAsync();
        }

        [Command("reannounce", RunMode = RunMode.Async)]
        [Description("Reschedule an announcement. Usage: `~reannounce Old Time | New Time`")]
        public async Task Reannounce([Remainder] string args)
        {
            var outputChannel = GetOutputChannel();
            if (outputChannel == null) return;

            var username = Context.User.ToString();
            var times = args.Split('|');
            
            var curTime = Util.GetDateTime(times[0]);
            if (curTime < DateTime.Now)
            {
                await ReplyAsync("The first time is in the past!");
                return;
            }

            var newTime = Util.GetDateTime(times[1]);
            if (newTime < DateTime.Now)
            {
                await ReplyAsync("The second time is in the past!");
                return;
            }

            var (embedMessage, embed) = await FindAnnouncement(outputChannel, username, curTime);
            if (embedMessage != null)
            {
                var tzi = TimeZoneInfo.FindSystemTimeZoneById(Util.PstIdString());
                var tzAbbrs = TZNames.GetAbbreviationsForTimeZone(tzi.Id, "en-US");
                var tzAbbr = tzi.IsDaylightSavingTime(DateTime.Now) ? tzAbbrs.Daylight : tzAbbrs.Standard;

                await embedMessage.ModifyAsync(props =>
                {
                    props.Embed = embed
                        .ToEmbedBuilder()
                        .WithTimestamp(newTime.AddHours(-tzi.BaseUtcOffset.Hours))
                        .WithTitle($"Event scheduled by {Context.User} on {newTime.DayOfWeek} at {newTime.ToShortTimeString()} ({tzAbbr})!")
                        .Build();
                });
                
#if DEBUG
                var @event = await FindEvent("drs", username, curTime.AddHours(-tzi.BaseUtcOffset.Hours));
                @event.StartTime = XmlConvert.ToString(newTime.AddHours(-tzi.BaseUtcOffset.Hours),
                    XmlDateTimeSerializationMode.Utc);
                await Calendar.UpdateEvent("drs", @event);
#else
                var @event = await FindEvent(GetCalendarCode(outputChannel.Id), username, curTime.AddHours(-tzi.BaseUtcOffset.Hours));
                @event.StartTime = XmlConvert.ToString(newTime.AddHours(-tzi.BaseUtcOffset.Hours),
                    XmlDateTimeSerializationMode.Utc);
                await Calendar.UpdateEvent(GetCalendarCode(outputChannel.Id), @event);
#endif

                await SortEmbeds(outputChannel);

                Log.Information("Rescheduled announcement from {OldTime} to {NewTime}", curTime.ToShortTimeString(), newTime.ToShortTimeString());
                await ReplyAsync("Announcement rescheduled!");
            }
            else
            {
                await ReplyAsync("No announcement by you was found at that time!");
            }
        }

        [Command("unannounce", RunMode = RunMode.Async)]
        [Description("Cancel an event. Usage: `~unannounce Time`")]
        public async Task Unannounce([Remainder]string args)
        {
            var outputChannel = GetOutputChannel();
            if (outputChannel == null) return;

            var username = Context.User.ToString();
            var time = Util.GetDateTime(args);
            if (time < DateTime.Now)
            {
                await ReplyAsync("That time is in the past!");
                return;
            }

            var tzi = TimeZoneInfo.FindSystemTimeZoneById(Util.PstIdString());

            var (embedMessage, embed) = await FindAnnouncement(outputChannel, username, time);
            if (embedMessage != null)
            {
                await embedMessage.ModifyAsync(props =>
                {
                    props.Embed = new EmbedBuilder()
                        .WithTitle(embed.Title)
                        .WithColor(embed.Color.Value)
                        .WithDescription("❌ Cancelled")
                        .Build();
                });

                new Task(async () => {
                    await Task.Delay(1000 * 60 * 60 * 2); // 2 hours
                    await embedMessage.DeleteAsync();
                }).Start();
                
#if DEBUG
                var @event = await FindEvent("drs", username, time.AddHours(-tzi.BaseUtcOffset.Hours));
                await Calendar.DeleteEvent("drs", @event.ID);
#else
                var @event = await FindEvent(GetCalendarCode(outputChannel.Id), username, time.AddHours(-tzi.BaseUtcOffset.Hours));
                await Calendar.DeleteEvent(GetCalendarCode(outputChannel.Id), @event.ID);
#endif

                await ReplyAsync("Event cancelled.");
            }
            else
            {
                await ReplyAsync("No event by you was found at that time!");
            }
        }

        private string GetCalendarCode(ulong channelId)
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return null;

            if (channelId == guildConfig.CastrumScheduleOutputChannel)
                return "cll";
            else if (channelId == guildConfig.DelubrumScheduleOutputChannel)
                return "drs";
            else if (channelId == guildConfig.DelubrumNormalScheduleOutputChannel)
                return "dr";
            else
                return null;
        }

        private IMessageChannel GetOutputChannel()
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return null;
            if (Context.Channel.Id != guildConfig.CastrumScheduleInputChannel
                && Context.Channel.Id != guildConfig.DelubrumScheduleInputChannel
                && Context.Channel.Id != guildConfig.DelubrumNormalScheduleInputChannel) return null;

            ulong outputChannelId;
            if (Context.Channel.Id == guildConfig.CastrumScheduleInputChannel)
            {
                outputChannelId = guildConfig.CastrumScheduleOutputChannel;
            }
            else if (Context.Channel.Id == guildConfig.DelubrumScheduleInputChannel)
            {
                outputChannelId = guildConfig.DelubrumScheduleOutputChannel;
            }
            else // Context.Channel.Id == guildConfig.DelubrumNormalScheduleInputChannel
            {
                outputChannelId = guildConfig.DelubrumNormalScheduleOutputChannel;
            }

            return Context.Guild.GetTextChannel(outputChannelId);
        }

        private static async Task<(IUserMessage, IEmbed)> FindAnnouncement(IMessageChannel channel, string username, DateTime time)
        {
            await foreach (var page in channel.GetMessagesAsync())
            {
                foreach (var message in page)
                {
                    var restMessage = (IUserMessage)message;

                    var embed = restMessage.Embeds.FirstOrDefault();
                    if (embed == null) continue;

                    if (!(embed.Title.Contains(username) && embed.Title.Contains(time.ToShortTimeString()))) continue;

                    return (restMessage, embed);
                }
            }

            return (null, null);
        }
    }
}
