using Discord;
using Discord.WebSocket;
using Prima.Models;
using Prima.Resources;
using Prima.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeZoneNames;

namespace Prima.Scheduler
{
    public static class ScheduleUtils
    {
        public static async Task<IEnumerable<(IMessage, IEmbed)>> GetEvents(IMessageChannel channel)
        {
            var events = new List<(IMessage, IEmbed)>();
            if (channel != null)
            {
                await foreach (var page in channel.GetMessagesAsync())
                {
                    // ReSharper disable once LoopCanBeConvertedToQuery
                    foreach (var message in page)
                    {
                        var embed = message.Embeds.FirstOrDefault(e => e.Type == EmbedType.Rich);
                        events.Add((message, embed));
                    }
                }
            }

            return events;
        }

        public static IMessageChannel GetOutputChannel(DiscordGuildConfiguration guildConfig, SocketGuild guild, IMessageChannel inputChannel)
        {
            ulong outputChannelId;
            if (inputChannel.Id == guildConfig.ScheduleInputChannel)
                outputChannelId = guildConfig.ScheduleOutputChannel;
            else if (inputChannel.Id == guildConfig.SocialScheduleInputChannel)
                outputChannelId = guildConfig.SocialScheduleOutputChannel;
            else if (inputChannel.Id == guildConfig.ZadnorThingScheduleInputChannel)
                outputChannelId = guildConfig.ZadnorThingScheduleOutputChannel;
            else if (inputChannel.Id == guildConfig.CastrumScheduleInputChannel)
                outputChannelId = guildConfig.CastrumScheduleOutputChannel;
            else if (inputChannel.Id == guildConfig.BozjaClusterScheduleInputChannel)
                outputChannelId = guildConfig.BozjaClusterScheduleOutputChannel;
            else if (inputChannel.Id == guildConfig.DelubrumScheduleInputChannel)
                outputChannelId = guildConfig.DelubrumScheduleOutputChannel;
            else // inputChannel.Id == guildConfig.DelubrumNormalScheduleInputChannel
                outputChannelId = guildConfig.DelubrumNormalScheduleOutputChannel;

            return guild.GetTextChannel(outputChannelId);
        }

        public static string GetCalendarCodeForOutputChannel(DiscordGuildConfiguration guildConfig, ulong channelId)
        {
            if (guildConfig == null) return null;

            if (channelId == guildConfig.CastrumScheduleOutputChannel)
                return "cll";
            if (channelId == guildConfig.BozjaClusterScheduleOutputChannel)
                return "bcf";
            if (channelId == guildConfig.DelubrumScheduleOutputChannel)
                return "drs";
            if (channelId == guildConfig.DelubrumNormalScheduleOutputChannel)
                return "dr";
            if (channelId == guildConfig.ZadnorThingScheduleOutputChannel)
                return "zad";
            if (channelId == guildConfig.ScheduleOutputChannel)
                return "ba";
            return channelId == guildConfig.SocialScheduleOutputChannel ? "social" : null;
        }

        private static bool MightBeTimeZone(string x)
            => x.ToUpperInvariant().EndsWith("T") && x.Length is <= 4 and > 1;

        private static TimeZoneInfo CreateTimeZone(string id, double utcOffset, string displayName)
            => TimeZoneInfo.CreateCustomTimeZone(id, TimeSpan.FromHours(utcOffset), displayName, displayName);

        public static TimeZoneInfo TimeZoneFromAbbr(string abbr)
        {
            abbr = abbr.ToUpperInvariant();
            return abbr switch
            {
                // TODO: Invariant time zones like PT and ET
                "HST" => CreateTimeZone("X_HST", -10, "Hawaiian Standard Time"),
                "AKST" => CreateTimeZone("X_AKST", -9, "Alaskan Standard Time"),
                "AKDT" => CreateTimeZone("X_AKDT", -8, "Alaskan Daylight Time"),
                "PST" => CreateTimeZone("X_PST", -8, "Pacific Standard Time"),
                "PDT" => CreateTimeZone("X_PDT", -7, "Pacific Daylight Time"),
                "MST" => CreateTimeZone("X_MST", -7, "Mountain Standard Time"),
                "MDT" => CreateTimeZone("X_MDT", -6, "Mountain Daylight Time"),
                "CST" => CreateTimeZone("X_CST", -6, "Central Standard Time"),
                "CDT" => CreateTimeZone("X_CDT", -5, "Central Daylight Time"),
                "EST" => CreateTimeZone("X_EST", -5, "Eastern Standard Time"),
                "EDT" => CreateTimeZone("X_EDT", -4, "Eastern Daylight Time"),
                _ => throw new ArgumentException("The specified time zone is not currently supported."),
            };
        }

        /// <summary>
        /// Gets a day of the week and a time from a set of strings.
        ///
        /// All this is copied from Roo's scheduler (with minor tweaks).
        /// </summary>
        public static (DateTime, TimeZoneInfo) GetDateTime(string keywords)
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;
            var day = DateTime.Now.Day;
            var hour = DateTime.Now.Hour;
            var minute = DateTime.Now.Minute;
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(Util.PstIdString());
            var dayOfWeek = -1;

            // Check to see if it matches a recognized time format
            var timeResult = RegexSearches.Time.Match(string.Join(' ', keywords));
            if (timeResult.Success)
            {
                var time = timeResult.Value.ToLowerInvariant().Replace(" ", "");
                hour = int.Parse(RegexSearches.TimeHours.Match(time).Value);

                var minuteMatch = RegexSearches.TimeMinutes.Match(time);
                minute = int.Parse(minuteMatch.Success ? minuteMatch.Value : "0");

                var meridiem = RegexSearches.TimeMeridiem.Match(time).Value;

                if (!meridiem.StartsWith("a") && hour != 12)
                {
                    hour += 12;
                }

                if (meridiem.StartsWith("a") && hour == 12)
                {
                    hour = 0;
                }
            }

            var splitKeywords = RegexSearches.Whitespace.Split(keywords);

            foreach (var keyword in splitKeywords)
            {
                // Check to see if it matches a recognized time zone
                if (MightBeTimeZone(keyword))
                {
                    try
                    {
                        timeZone = TimeZoneFromAbbr(keyword);
                    }
                    catch (ArgumentException) { /* ignore */ }
                }

                // Check to see if it matches a recognized date format
                var dateResult = RegexSearches.Date.Match(keyword);
                if (dateResult.Success)
                {
                    var date = dateResult.Value.Trim();
                    var mmddyyyy = date.Split("/").Select(int.Parse).ToArray();
                    month = mmddyyyy[0];
                    day = mmddyyyy[1];
                    if (mmddyyyy.Length == 3)
                    {
                        year = mmddyyyy[2];
                    }
                    continue;
                }

                // Check for days of the week, possibly abbreviated
                if (dayOfWeek == -1)
                {
                    dayOfWeek = keyword.ToLowerInvariant() switch
                    {
                        "日" or "日曜日" or "su" or "sun" or "sunday" => (int)DayOfWeek.Sunday,
                        "月" or "月曜日" or "m" or "mo" or "mon" or "monday" => (int)DayOfWeek.Monday,
                        "火" or "火曜日" or "t" or "tu" or "tue" or "tues" or "tuesday" => (int)DayOfWeek.Tuesday,
                        "水" or "水曜日" or "w" or "wed" or "wednesday" => (int)DayOfWeek.Wednesday,
                        "木" or "木曜日" or "th" or "thu" or "thursday" => (int)DayOfWeek.Thursday,
                        "金" or "金曜日" or "f" or "fri" or "friday" => (int)DayOfWeek.Friday,
                        "土" or "土曜日" or "sa" or "sat" or "saturday" => (int)DayOfWeek.Saturday,
                        _ => -1,
                    };
                }
            }

            // Check to make sure everything got set here, and then...
            var finalDate = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
            if (dayOfWeek >= 0)
            {
                finalDate = finalDate.AddDays((dayOfWeek - (int)finalDate.DayOfWeek + 7) % 7);
            }

            return (finalDate, timeZone);
        }
    }
}