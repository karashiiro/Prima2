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

        public static async Task RebuildPosts(IDbService db, DiscordSocketClient client, ulong guildId)
        {
            var guildConfig = db.Guilds.FirstOrDefault(g => g.Id == guildId);
            if (guildConfig == null)
                return;

            var guild = client.GetGuild(guildId);

            var iInChannel = client.GetChannel(guildConfig.ScheduleInputChannel);
            if (iInChannel is not ITextChannel inChannel)
                return;

            var iOutChannel = client.GetChannel(guildConfig.ScheduleOutputChannel);
            if (iOutChannel is not ITextChannel outChannel)
                return;

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

                var iOutMessage = await outChannel.GetMessageAsync(run.EmbedMessageId);
                if (iOutMessage is not IUserMessage message)
                {
                    Log.Information("Embed message {MessageId} is invalid; skipping...", run.EmbedMessageId);
                    continue;
                }

                var leader = guild.GetUser(originalMessage.Author.Id);
                if (leader == null)
                {
                    Log.Information("Leader {UserId} is null; skipping...", originalMessage.Author.Id);
                    continue;
                }
                var leaderName = leader.Nickname ?? leader.ToString();

                var runTime = DateTime.FromBinary(run.RunTime);

                var tzi = TimeZoneInfo.FindSystemTimeZoneById(Util.PstIdString());

                var timeOffset = new DateTimeOffset(runTime.AddHours(-tzi.BaseUtcOffset.Hours));
                var newEmbed = message.Embeds.FirstOrDefault()?.ToEmbedBuilder()
                    .WithTitle(
                        $"Run scheduled by {leaderName} at <t:{timeOffset.ToUnixTimeSeconds()}:F>!")
                    .WithDescription("React to the :vibration_mode: on their message to be notified 30 minutes before it begins!\n\n" +
                                     $"**{guild.GetUser(run.LeaderId).Mention}'s full message: {originalMessage.GetJumpUrl()}**\n\n" +
                                     $"{new string(run.Description.Take(1650).ToArray())}{(run.Description.Length > 1650 ? "..." : "")}\n\n" +
                                     $"**Schedule Overview: <{guildConfig.BASpreadsheetLink}>**")
                    .WithTimestamp(timeOffset)
                    .Build();

                if (newEmbed == null)
                {
                    Log.Information("newEmbed is null; skipping...");
                    continue;
                }
                await message.ModifyAsync(properties => properties.Embeds = new[] { newEmbed });
                Log.Information("Updated information for run {MessageId}", run.MessageId3);
                postsUpdated++;
            }

            Log.Information("Done; {PostsUpdated} messages updated.", postsUpdated);
        }
    }
}