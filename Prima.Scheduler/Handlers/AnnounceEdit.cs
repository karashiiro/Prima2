using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Discord;
using Discord.WebSocket;
using Prima.Models;
using Prima.Resources;
using Prima.Scheduler.GoogleApis.Calendar;
using Prima.Scheduler.GoogleApis.Services;
using Prima.Services;
using Serilog;
using TimeZoneNames;

namespace Prima.Scheduler.Handlers
{
    public static class AnnounceEdit
    {
        public static async Task Handler(DiscordSocketClient client, CalendarApi calendar, DbService db, SocketMessage message)
        {
            var guildConfig = db.Guilds.FirstOrDefault(g => g.Id == SpecialGuilds.CrystalExploratoryMissions);
            if (guildConfig == null)
            {
                Log.Error("No guild configuration found for the default guild!");
                return;
            }

            var guild = client.GetGuild(guildConfig.Id);

            var prefix = db.Config.Prefix;

            var outputChannel = GetOutputChannel(guildConfig, guild, message.Channel);
            if (outputChannel == null) return;

            var args = message.Content.Substring(message.Content.IndexOf(' ') + 1);

            var splitIndex = args.IndexOf("|", StringComparison.Ordinal);
            if (splitIndex == -1)
            {
                await message.Channel.SendMessageAsync(
                    $"{message.Author.Mention}, please provide parameters with that command.\n" +
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

            var time = Util.GetDateTime(parameters);
            if (time < DateTime.Now)
            {
                await message.Channel.SendMessageAsync("You cannot announce an event in the past!");
                return;
            }

            var tzi = TimeZoneInfo.FindSystemTimeZoneById(Util.PstIdString());
            var tzAbbrs = TZNames.GetAbbreviationsForTimeZone(tzi.Id, "en-US");
            var tzAbbr = tzi.IsDaylightSavingTime(DateTime.Now) ? tzAbbrs.Daylight : tzAbbrs.Standard;

#if DEBUG
            var @event = await FindEvent(calendar, "drs", message.Author.ToString(), time);
            await calendar.UpdateEvent("drs", new MiniEvent
#else
            var calendarCode = GetCalendarCode(outputChannel.Id);
            var @event = await FindEvent(calendar, calendarCode, message.Author.ToString(), time);
            await calendar.UpdateEvent(calendarCode, new MiniEvent
#endif
            {
                Title = message.Author.ToString(),
                Description = description,
                StartTime = XmlConvert.ToString(time.AddHours(-tzi.BaseUtcOffset.Hours), XmlDateTimeSerializationMode.Utc),
                ID = @event.ID,
            });

            var (embedMessage, embed) = await FindAnnouncement(outputChannel, message.Author.ToString(), time);
            var calendarLinkLine = embed.Description.Split('\n').Last();
            await embedMessage.ModifyAsync(props =>
            {
                props.Embed = embed
                    .ToEmbedBuilder()
                    .WithTimestamp(time.AddHours(-tzi.BaseUtcOffset.Hours))
                    .WithTitle($"Event scheduled by {message.Author} on {time.DayOfWeek} at {time.ToShortTimeString()} ({tzAbbr})!")
                    .WithDescription(trimmedDescription + (calendarLinkLine.StartsWith("[Copy to Google Calendar]")
                        ? $"\n\n{calendarLinkLine}"
                        : ""))
                    .Build();
            });
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

        private static async Task<MiniEvent> FindEvent(CalendarApi calendar, string calendarClass, string title, DateTime startTime)
        {
            var tzi = TimeZoneInfo.FindSystemTimeZoneById(Util.PstIdString());
            var events = await calendar.GetEvents(calendarClass);
            return events.FirstOrDefault(e =>
            {
                var eventStartTime = XmlConvert.ToDateTime(e.StartTime, XmlDateTimeSerializationMode.Utc).AddHours(tzi.BaseUtcOffset.Hours);
                return e.Title == title && eventStartTime == startTime;
            });
        }

        private static IMessageChannel GetOutputChannel(DiscordGuildConfiguration guildConfig, SocketGuild guild, IMessageChannel inputChannel)
        {
            ulong outputChannelId;
            if (inputChannel.Id == guildConfig.CastrumScheduleInputChannel)
            {
                outputChannelId = guildConfig.CastrumScheduleOutputChannel;
            }
            else if (inputChannel.Id == guildConfig.DelubrumScheduleInputChannel)
            {
                outputChannelId = guildConfig.DelubrumScheduleOutputChannel;
            }
            else // inputChannel.Id == guildConfig.DelubrumNormalScheduleInputChannel
            {
                outputChannelId = guildConfig.DelubrumNormalScheduleOutputChannel;
            }

            return guild.GetTextChannel(outputChannelId);
        }
    }
}