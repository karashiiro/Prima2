using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Discord;
using Discord.Commands;
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
        public static async Task Handler(DiscordSocketClient client, CalendarApi calendar, IDbService db, SocketMessage message)
        {
            var guildConfig = db.Guilds.FirstOrDefault(g => g.Id == SpecialGuilds.CrystalExploratoryMissions);
            if (guildConfig == null)
            {
                Log.Error("No guild configuration found for the default guild!");
                return;
            }

            var guild = client.GetGuild(guildConfig.Id);

            var prefix = db.Config.Prefix;
            if (message.Content == null || !message.Content.StartsWith(prefix + "announce")) return;

            Log.Information("Announcement message being edited.");

            var outputChannel = ScheduleUtils.GetOutputChannel(guildConfig, guild, message.Channel);
            if (outputChannel == null)
            {
                Log.Information("Could not get output channel; aborting.");
                return;
            }

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
            var trimmedDescription = description.Substring(0, Math.Min(1700, description.Length));
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
            var isDST = tzi.IsDaylightSavingTime(DateTime.Now);
            var tzAbbr = isDST ? tzAbbrs.Daylight : tzAbbrs.Standard;
            var timeMod = -tzi.BaseUtcOffset.Hours;
            if (isDST)
                timeMod -= 1;

#if DEBUG
            var @event = await FindEvent(calendar, "drs", message.Author.ToString(), time.AddHours(timeMod));
#else
            var calendarCode = ScheduleUtils.GetCalendarCodeForOutputChannel(guildConfig, outputChannel.Id);
            var @event = await FindEvent(calendar, calendarCode, message.Author.ToString(), time.AddHours(timeMod));
#endif

            if (@event != null)
            {
#if DEBUG
                await calendar.UpdateEvent("drs", new MiniEvent
#else
                await calendar.UpdateEvent(calendarCode, new MiniEvent
#endif
                {
                    Title = message.Author.ToString(),
                    Description = description,
                    StartTime = XmlConvert.ToString(time.AddHours(timeMod), XmlDateTimeSerializationMode.Utc),
                    ID = @event.ID,
                });

                Log.Information("Updated calendar entry.");
            }

            var (embedMessage, embed) = await FindAnnouncement(outputChannel, message.Id);
            var lines = embed.Description.Split('\n');
            var messageLinkLine = lines.LastOrDefault(l => l.StartsWith("Message Link: https://discordapp.com/channels/"));
            var calendarLinkLine = lines.LastOrDefault(l => l.StartsWith("[Copy to Google Calendar]"));
            var member = guild.GetUser(message.Author.Id);
            await embedMessage.ModifyAsync(props =>
            {
                props.Embed = embed
                    .ToEmbedBuilder()
                    .WithTimestamp(time.AddHours(timeMod))
                    .WithTitle($"Event scheduled by {member?.Nickname ?? message.Author.ToString()} on {time.DayOfWeek} at {time.ToShortTimeString()} ({tzAbbr})!")
                    .WithDescription(trimmedDescription + (calendarLinkLine != null
                        ? $"\n\n{calendarLinkLine}"
                        : "") + (messageLinkLine != null
                        ? $"\n{messageLinkLine}"
                        : ""))
                    .Build();
            });

            Log.Information("Updated announcement embed.");
        }

        private static async Task<(IUserMessage, IEmbed)> FindAnnouncement(IMessageChannel channel, ulong eventId)
        {
            await foreach (var page in channel.GetMessagesAsync())
            {
                foreach (var message in page)
                {
                    var restMessage = (IUserMessage)message;

                    var embed = restMessage.Embeds.FirstOrDefault();
                    if (embed?.Footer == null) continue;

                    if (embed.Footer?.Text != eventId.ToString()) continue;

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
                var eventStartTime = XmlConvert.ToDateTime(e.StartTime, XmlDateTimeSerializationMode.Utc);
                return e.Title == title && eventStartTime == startTime;
            });
        }
    }
}