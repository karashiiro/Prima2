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

        private static TimeZoneInfo GetTimeZone(string id)
            => TimeZoneInfo.FindSystemTimeZoneById(id);

        public static TimeZoneInfo TimeZoneFromAbbr(string abbr)
        {
            abbr = abbr.ToUpperInvariant();
            return abbr switch
            {
                "HST" => GetTimeZone(Util.IsUnix() ? "America/Honolulu" : "Hawaiian Standard Time"),
                "AKDT" or "AKST" => GetTimeZone(Util.IsUnix() ? "America/Anchorage" : "Alaskan Standard Time"),
                "PDT" or "PST" => GetTimeZone(Util.IsUnix() ? "America/Los_Angeles" : "Pacific Standard Time"),
                "MDT" or "MST" => GetTimeZone(Util.IsUnix() ? "America/Phoenix" : "Mountain Standard Time"),
                "CDT" or "CST" => GetTimeZone(Util.IsUnix() ? "America/Chicago" : "Central Standard Time"),
                "EDT" or "EST" => GetTimeZone(Util.IsUnix() ? "America/New_York" : "Eastern Standard Time"),
                _ => throw new ArgumentException("The specified time zone is not currently supported."),
            };
        }
    }
}