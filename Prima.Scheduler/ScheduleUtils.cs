using DSharpPlus;
using DSharpPlus.Entities;
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
        public static async Task<IEnumerable<(DiscordMessage, DiscordEmbed)>> GetEvents(DiscordChannel channel)
        {
            var events = new List<(DiscordMessage, DiscordEmbed)>();
            if (channel != null)
            {
                foreach (var message in await channel.GetMessagesAsync())
                {
                    var embed = message.Embeds.FirstOrDefault();
                    events.Add((message, embed));
                }
            }

            return events;
        }

        public static DiscordChannel GetOutputChannel(DiscordGuildConfiguration guildConfig, DiscordGuild guild, DiscordChannel inputChannel)
        {
            ulong outputChannelId;
            if (inputChannel.Id == guildConfig.SocialScheduleInputChannel)
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

            return guild.GetChannel(outputChannelId);
        }

        public static string GetCalendarCodeForOutputChannel(DiscordGuildConfiguration guildConfig, ulong channelId)
        {
            if (guildConfig == null) return null;

            if (channelId == guildConfig.CastrumScheduleOutputChannel)
                return "cll";
            else if (channelId == guildConfig.BozjaClusterScheduleOutputChannel)
                return "bcf";
            else if (channelId == guildConfig.DelubrumScheduleOutputChannel)
                return "drs";
            else if (channelId == guildConfig.DelubrumNormalScheduleOutputChannel)
                return "dr";
            else if (channelId == guildConfig.ZadnorThingScheduleOutputChannel)
                return "zad";
            else if (channelId == guildConfig.SocialScheduleOutputChannel)
                return "social";
            else
                return null;
        }

        public static TimeZoneInfo TimeZoneFromAbbr(string abbr)
        {
            abbr = abbr.ToLowerInvariant();
            return TimeZoneInfo.GetSystemTimeZones()
                .FirstOrDefault(tzi =>
                {
                    var tzAbbrs = TZNames.GetAbbreviationsForTimeZone(tzi.Id, "en-US");
                    return tzAbbrs.Daylight?.ToLowerInvariant() == abbr
                           || tzAbbrs.Standard?.ToLowerInvariant() == abbr
                           || tzAbbrs.Generic?.ToLowerInvariant() == abbr;
                });
        }

        public static async Task RebuildPosts(IDbService db, DiscordClient client, ulong guildId)
        {
            var guildConfig = db.Guilds.FirstOrDefault(g => g.Id == guildId);
            if (guildConfig == null)
                return;

            var guild = await client.GetGuildAsync(guildId);

            var inChannel = await client.GetChannelAsync(guildConfig.ScheduleInputChannel);
            var outChannel = await client.GetChannelAsync(guildConfig.ScheduleOutputChannel);

            var tzi = TimeZoneInfo.FindSystemTimeZoneById(Util.PstIdString());
            var tzAbbrs = TZNames.GetAbbreviationsForTimeZone(tzi.Id, "en-US");
            var tzAbbr = tzi.IsDaylightSavingTime(DateTime.Now) ? tzAbbrs.Daylight : tzAbbrs.Standard;

            var postsUpdated = 0;

            var postsToUpdate = db.Events.Where(e => !e.Notified && e.RunKindCastrum == RunDisplayTypeCastrum.None);
            foreach (var run in postsToUpdate)
            {
                var originalMessage = await inChannel.GetMessageAsync(run.MessageId3);
                if (originalMessage == null)
                {
                    Log.Information("Original message is invalid; skipping...");
                    continue;
                }

                var outMessage = await outChannel.GetMessageAsync(run.EmbedMessageId);
                var leader = await guild.GetMemberAsync(originalMessage.Author.Id);
                if (leader == null)
                {
                    Log.Information("Leader {UserId} is null; skipping...", originalMessage.Author.Id);
                    continue;
                }
                var leaderName = leader.Nickname ?? leader.ToString();

                var runTime = DateTime.FromBinary(run.RunTime);

                var embed = outMessage.Embeds.FirstOrDefault();
                if (embed == null)
                {
                    Log.Information("newEmbed is null; skipping...");
                    continue;
                }

                var newEmbed = new DiscordEmbedBuilder(embed)
                    .WithTitle(
                        $"Run scheduled by {leaderName} on {runTime.DayOfWeek} at {runTime.ToShortTimeString()} ({tzAbbr}) " +
                        $"[{runTime.DayOfWeek}, {(Month)runTime.Month} {runTime.Day}]!")
                    .WithDescription("React to the :vibration_mode: on their message to be notified 30 minutes before it begins!\n\n" +
                                     $"**<@{run.LeaderId}>'s full message: {originalMessage.JumpLink}**\n\n" +
                                     $"{new string(run.Description.Take(1650).ToArray())}{(run.Description.Length > 1650 ? "..." : "")}\n\n" +
                                     $"**Schedule Overview: <{guildConfig.BASpreadsheetLink}>**")
                    .WithTimestamp(runTime.AddHours(-tzi.BaseUtcOffset.Hours))
                    .Build();

                await outMessage.ModifyAsync(properties => properties.Embed = newEmbed);
                Log.Information("Updated information for run {MessageId}", run.MessageId3);
                postsUpdated++;
            }

            Log.Information("Done; {PostsUpdated} messages updated.", postsUpdated);
        }
    }
}