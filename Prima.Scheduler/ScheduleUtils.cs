using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Prima.Resources;
using Prima.Services;
using Serilog;
using TimeZoneNames;

namespace Prima.Scheduler
{
    public static class ScheduleUtils
    {
        public static async Task RebuildPosts(DbService db, DiscordSocketClient client, ulong guildId)
        {
            var guildConfig = db.Guilds.FirstOrDefault(g => g.Id == guildId);
            if (guildConfig == null)
                return;

            var guild = client.GetGuild(guildId);

            var channel = client.GetChannel(guildConfig.ScheduleOutputChannel);
            if (!(channel is ITextChannel textChannel))
                return;

            var tzi = TimeZoneInfo.FindSystemTimeZoneById(Util.PstIdString());
            var tzAbbrs = TZNames.GetAbbreviationsForTimeZone(tzi.Id, "en-US");
            var tzAbbr = tzi.IsDaylightSavingTime(DateTime.Now) ? tzAbbrs.Daylight : tzAbbrs.Standard;

            var postsUpdated = 0;

            var postsToUpdate = db.Events.Where(e => !e.Notified && e.RunKindCastrum == RunDisplayTypeCastrum.None);
            foreach (var run in postsToUpdate)
            {
                var originalMessage = await textChannel.GetMessageAsync(run.MessageId3);
                var imessage = await textChannel.GetMessageAsync(run.EmbedMessageId);
                if (!(imessage is SocketUserMessage message))
                {
                    Log.Information("Embed message {MessageId} is null; skipping...", run.EmbedMessageId);
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

                var newEmbed = message.Embeds.FirstOrDefault()?.ToEmbedBuilder()
                    .WithTitle(
                        $"Run scheduled by {leaderName} on {runTime.DayOfWeek} at {runTime.ToShortTimeString()} ({tzAbbr}) " +
                        $"[{runTime.DayOfWeek}, {(Month)runTime.Month} {runTime.Day}]!")
                    .WithDescription("React to the :vibration_mode: on their message to be notified 30 minutes before it begins!\n\n" +
                                     $"**{guild.GetUser(run.LeaderId).Mention}'s full message: {originalMessage.GetJumpUrl()}**\n\n" +
                                     $"{new string(run.Description.Take(1650).ToArray())}{(run.Description.Length > 1650 ? "..." : "")}\n\n" +
                                     $"**Schedule Overview: <{guildConfig.BASpreadsheetLink}>**")
                    .WithTimestamp(runTime.AddHours(tzi.BaseUtcOffset.Hours))
                    .Build();

                if (newEmbed == null)
                {
                    Log.Information("newEmbed is null; skipping...");
                    continue;
                }
                await message.ModifyAsync(properties => properties.Embed = newEmbed);
                Log.Information("Updated information for run {MessageId}", run.MessageId3);
                postsUpdated++;
            }

            Log.Information("Done; {PostsUpdated} messages updated.", postsUpdated);
        }
    }
}