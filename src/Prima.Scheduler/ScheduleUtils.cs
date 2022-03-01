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

        private static TimeZoneInfo CreateTimeZone(string id, double utcOffset, string displayName)
            => TimeZoneInfo.CreateCustomTimeZone(id, TimeSpan.FromHours(utcOffset), displayName, displayName);

        public static TimeZoneInfo TimeZoneFromAbbr(string abbr)
        {
            abbr = abbr.ToUpperInvariant();
            return abbr switch
            {
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
    }
}