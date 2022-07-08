﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Prima.DiscordNet.Attributes;
using Prima.Models;
using Prima.Resources;
using Prima.Scheduler.GoogleApis.Calendar;
using Prima.Scheduler.GoogleApis.Services;
using Prima.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Color = Discord.Color;
// ReSharper disable MemberCanBePrivate.Global

namespace Prima.Scheduler.Modules
{
    [Name("NotificationBoard")]
    [RequireContext(ContextType.Guild)]
    public class NotificationBoardModule : ModuleBase<SocketCommandContext>
    {
        public CalendarApi Calendar { get; set; }
        public IDbService Db { get; set; }

        [Command("announce", RunMode = RunMode.Async)]
        [Description("Announce an event. Usage: `~announce Time | Description`")]
        public async Task Announce([Remainder] string args)
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return;

            var outputChannel = ScheduleUtils.GetOutputChannel(guildConfig, Context.Guild, Context.Channel);
            if (outputChannel == null) return;

            var announceChannel = ScheduleUtils.GetAnnouncementChannel(guildConfig, Context.Guild, Context.Channel);

            var prefix = Db.Config.Prefix;

            var splitIndex = args.IndexOf("|", StringComparison.Ordinal);
            if (splitIndex == -1)
            {
                await ReplyAsync($"{Context.User.Mention}, please provide parameters with that command.\n" +
                                 "A well-formed command would look something like:\n" +
                                 $"`{prefix}announce 5:00PM | This is a fancy description!`");
                return;
            }

            var parameters = args[..splitIndex].Trim();
            var description = args[(splitIndex + 1)..].Trim();
            var trimmedDescription = description[..Math.Min(1700, description.Length)];
            if (trimmedDescription.Length != description.Length)
            {
                trimmedDescription += "...";
            }

            if (!parameters.Contains(":"))
            {
                await ReplyAsync($"{Context.User.Mention}, please specify a time for your run in your command!");
                return;
            }

            var (time, tzi) = ScheduleUtils.ParseTime(parameters);
            var isDST = tzi.IsDaylightSavingTime(time);
            if (isDST)
                time = time.AddHours(-1);
            if (time < DateTimeOffset.Now)
            {
                await ReplyAsync("You cannot announce an event in the past!");
                return;
            }

            var eventLink =
#if DEBUG
            await Calendar.PostEvent("drs", new MiniEvent
#else
            await Calendar.PostEvent(ScheduleUtils.GetCalendarCodeForOutputChannel(guildConfig, outputChannel.Id), new MiniEvent
#endif
            {
                Title = Context.User.ToString(),
                Description = description,
                StartTime = XmlConvert.ToString(time.UtcDateTime, XmlDateTimeSerializationMode.Utc),
            });

            var eventDescription = trimmedDescription +
                                   $"\n\n[Copy to Google Calendar]({eventLink})\nMessage Link: {Context.Message.GetJumpUrl()}";
            
            var member = Context.Guild.GetUser(Context.User.Id);
            var color = RunDisplayTypes.GetColorCastrum();
            var embed = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithIconUrl(Context.User.GetAvatarUrl())
                    .WithName(Context.User.ToString()))
                .WithColor(new Color(color.RGB[0], color.RGB[1], color.RGB[2]))
                .WithTimestamp(time)
                .WithTitle(
                    $"Event scheduled by {member?.Nickname ?? Context.User.ToString()} at <t:{time.ToUnixTimeSeconds()}:F>!")
                .WithDescription(eventDescription)
                .WithFooter(Context.Message.Id.ToString())
                .Build();
            
            await outputChannel.SendMessageAsync(Context.Message.Id.ToString(), embed: embed);
            if (announceChannel != null)
            {
                var announceMessage = await announceChannel.SendMessageAsync(Context.Message.Id.ToString(), embed: embed);
                await announceMessage.CrosspostAsync();
            }

            await ReplyAsync($"Event announced! Announcement posted in <#{outputChannel.Id}>. React to the announcement in " +
                             $"<#{outputChannel.Id}> with :vibration_mode: to be notified before the event begins.");
            await SortEmbeds(guildConfig, Context.Guild, outputChannel);
        }

        private async Task<MiniEvent> FindEvent(string calendarClass, string title, DateTimeOffset startTime)
        {
            var events = await Calendar.GetEvents(calendarClass);
            return events.FirstOrDefault(e =>
            {
                var eventStartTime = XmlConvert.ToDateTimeOffset(e.StartTime);
                return e.Title == title && eventStartTime == startTime;
            });
        }

        [Command("setruntime")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireContext(ContextType.Guild)]
        public async Task SetRunTimestamp(ulong outputChannelId, ulong eventId, [Remainder] string args)
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return;
            
            var outputChannel = Context.Guild.GetTextChannel(outputChannelId);
            var (embedMessage, embed) = await FindAnnouncement(outputChannel, eventId);
            if (embed == null)
            {
                await ReplyAsync("No run was found matching that event ID in that channel.");
                return;
            }
            
            var announceChannel = ScheduleUtils.GetAnnouncementChannel(guildConfig, Context.Guild, Context.Channel);
            var (announceMessage, _) = announceChannel != null
                ? await FindAnnouncement(announceChannel, eventId)
                : (null, null);

            var (newRunTime, tzi) = ScheduleUtils.ParseTime(args);
            var isDST = tzi.IsDaylightSavingTime(newRunTime);
            if (isDST)
                newRunTime = newRunTime.AddHours(-1);

            var host = Context.Guild.Users.FirstOrDefault(u => u.ToString() == embed.Author?.Name);
            var builder = embed.ToEmbedBuilder();
            if (host != null)
            {
                builder = builder.WithTitle(
                    $"Event scheduled by {host.Nickname ?? host.ToString()} at <t:{newRunTime.ToUnixTimeSeconds()}:F> (Your local time)!");
            }

            var updatedEmbed = builder
                .WithTimestamp(newRunTime)
                .Build();
            
            await embedMessage.ModifyAsync(props =>
            {
                props.Embeds = new[] { updatedEmbed };
            });
            
            if (announceMessage != null)
            {
                await announceMessage.ModifyAsync(props =>
                {
                    props.Embeds = new[] { updatedEmbed };
                });
            }

            await ReplyAsync("Updated.");
        }

        [Command("sortembeds", RunMode = RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        [RequireOwner]
        public async Task SortEmbedsCommand(ulong id)
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return;

            var channel = Context.Guild.GetTextChannel(id);
            await SortEmbeds(guildConfig, Context.Guild, channel);
            await ReplyAsync("Done!");
        }

        private async Task SortEmbeds(DiscordGuildConfiguration guildConfig, IGuild guild, IMessageChannel channel)
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

            var dps = guild.Emotes.FirstOrDefault(e => e.Name.ToLowerInvariant() == "dps");
            var healer = guild.Emotes.FirstOrDefault(e => e.Name.ToLowerInvariant() == "healer");
            var tank = guild.Emotes.FirstOrDefault(e => e.Name.ToLowerInvariant() == "tank");
            foreach (var embed in embeds)
            {
                try
                {
                    var embedBuilder = embed.ToEmbedBuilder();

                    var lines = embed.Description.Split('\n');
                    var linkTrimmedDescription = lines
                        .Where(l => !LineContainsLastJumpUrl(l))
                        .Where(l => !LineContainsCalendarLink(l))
                        .Aggregate("", (agg, nextLine) => agg + nextLine + '\n')
                        .Trim();
                    var trimmedDescription = linkTrimmedDescription[..Math.Min(1700, linkTrimmedDescription.Length)].Trim();
                    if (trimmedDescription.Length != linkTrimmedDescription.Length)
                    {
                        trimmedDescription += "...";
                    }

                    var calendarLinkLine = lines.LastOrDefault(LineContainsCalendarLink);
                    var messageLinkLine =
                        $"Message Link: https://discordapp.com/channels/{guild.Id}/{Context.Channel.Id}/{embed.Footer?.Text}";

                    embedBuilder.WithDescription(trimmedDescription + (calendarLinkLine != null
                        ? $"\n\n{calendarLinkLine}"
                        : "") + $"\n{messageLinkLine}");

                    var host = Context.Guild.Users.FirstOrDefault(u => u.ToString() == embed.Author?.Name);
                    if (host != null && embed.Timestamp.HasValue)
                    {
                        var timeOffset = embed.Timestamp.Value;
                        embedBuilder.WithTitle(
                            $"Event scheduled by {host.Nickname ?? host.ToString()} at <t:{timeOffset.ToUnixTimeSeconds()}:F> (Your local time)!");
                    }

                    var m = await channel.SendMessageAsync(embed.Footer?.Text, embed: embedBuilder.Build());

                    try
                    {
                        await m.AddReactionAsync(new Emoji("📳"));

                        var noQueueChannels = new[]
                        {
                            guildConfig.BozjaClusterScheduleOutputChannel,
                            guildConfig.SocialScheduleOutputChannel,
                            guildConfig.DelubrumNormalScheduleOutputChannel,
                            guildConfig.DelubrumScheduleOutputChannel,

                            guildConfig.CastrumScheduleOutputChannel,
                            guildConfig.ScheduleOutputChannel, // BA
                        };

                        if (!noQueueChannels.Contains(channel.Id))
                            await m.AddReactionsAsync(new IEmote[] { dps, healer, tank });
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Failed to add reactions!");
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error in sorting procedure!");
                }
            }

            await progress.DeleteAsync();
        }

        private static bool LineContainsCalendarLink(string l)
        {
            return l.StartsWith("[Copy to Google Calendar]");
        }

        private static bool LineContainsLastJumpUrl(string l)
        {
            return l.StartsWith("Message Link: https://discord");
        }

        [Command("reactions", RunMode = RunMode.Async)]
        [Description("Get the number of reactions for an announcement.")]
        public async Task ReactionCount([Remainder] string args)
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return;

            var outputChannel = ScheduleUtils.GetOutputChannel(guildConfig, Context.Guild, Context.Channel);
            if (outputChannel == null) return;

            var (time, _) = ScheduleUtils.ParseTime(args);

            var (embedMessage, embed) = await FindAnnouncement(outputChannel, Context.User, time);
            if (embedMessage != null && embed?.Footer != null && ulong.TryParse(embed.Footer?.Text, out var originalMessageId))
            {
                var count = await Db.EventReactions
                    .Where(er => er.EventId == originalMessageId)
                    .CountAsync();
                await ReplyAsync($"That event has `{count}` reaction(s).");
            }
            else
            {
                await ReplyAsync("Failed to fetch embed message!");
            }
        }

        [Command("reannounce", RunMode = RunMode.Async)]
        [Description("Reschedule an announcement. Usage: `~reannounce Old Time | New Time`")]
        public async Task Reannounce([Remainder] string args)
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return;

            var outputChannel = ScheduleUtils.GetOutputChannel(guildConfig, Context.Guild, Context.Channel);
            if (outputChannel == null) return;
            
            var announceChannel = ScheduleUtils.GetAnnouncementChannel(guildConfig, Context.Guild, Context.Channel);

            var username = Context.User.ToString();
            var times = args.Split('|').Select(a => a.Trim()).ToArray();
            if (times.Length < 2)
            {
                await ReplyAsync("Failed to read command. Usage: `~reannounce Old Time Or Original Message ID | New Time`");
                return;
            }

            IUserMessage embedMessage;
            IUserMessage announceChannelMessage;
            IEmbed embed;
            DateTimeOffset curTime;

            // Read the second time
            var (newTime, _) = ScheduleUtils.ParseTime(times[1]);
            if (newTime < DateTimeOffset.Now)
            {
                await ReplyAsync("The second time is in the past!");
                return;
            }

            // Check if the user entered a message ID instead of a time
            if (times[0].Length > 8 && times[0].All(char.IsDigit))
            {
                if (!ulong.TryParse(times[0], out var announceMessageId))
                {
                    await ReplyAsync("Could not read message ID!");
                    return;
                }

                var announceMessage = await Context.Channel.GetMessageAsync(announceMessageId);
                if (announceMessage == null)
                {
                    await ReplyAsync("The message with that ID does not exist in this channel!");
                    return;
                }

                (embedMessage, embed) = await FindAnnouncementById(outputChannel, Context.User, times[0]);
                (announceChannelMessage, _) = await FindAnnouncementById(announceChannel, Context.User, times[0]);

                curTime = embed.Timestamp.Value;
            }
            else
            {
                // Read the first time
                var (tempTime, tzi) = ScheduleUtils.ParseTime(times[0]);
                curTime = tempTime;

                var isDST = tzi.IsDaylightSavingTime(curTime);
                if (isDST)
                    curTime = curTime.AddHours(-1);

                if (curTime < DateTimeOffset.Now)
                {
                    await ReplyAsync("The first time is in the past!");
                    return;
                }

                (embedMessage, embed) = await FindAnnouncement(outputChannel, Context.User, curTime);
                (announceChannelMessage, _) = await FindAnnouncement(announceChannel, Context.User, curTime);
            }

            if (embedMessage != null)
            {
                var member = Context.Guild.GetUser(Context.User.Id);
                var updatedEmbed = embed
                    .ToEmbedBuilder()
                    .WithTimestamp(newTime)
                    .WithTitle(
                        $"Event scheduled by {member?.Nickname ?? Context.User.ToString()} at <t:{newTime.ToUnixTimeSeconds()}:F>!")
                    .Build();
                
                await embedMessage.ModifyAsync(props =>
                {
                    props.Embeds = new[] { updatedEmbed };
                });

                if (announceChannelMessage != null)
                {
                    await announceChannelMessage.ModifyAsync(props =>
                    {
                        props.Embeds = new[] { updatedEmbed };
                    });
                }

#if DEBUG
                var @event = await FindEvent("drs", username, curTime);
                if (@event != null)
                {
                    @event.StartTime = XmlConvert.ToString(newTime.UtcDateTime,
                        XmlDateTimeSerializationMode.Utc);
                    await Calendar.UpdateEvent("drs", @event);
                }
                else
                {
                    Log.Warning("Failed to find calendar entry for event.");
                }
#else
                var @event = await FindEvent(ScheduleUtils.GetCalendarCodeForOutputChannel(guildConfig, outputChannel.Id), username, curTime);
                if (@event != null)
                {
                    @event.StartTime = XmlConvert.ToString(newTime.UtcDateTime,
                        XmlDateTimeSerializationMode.Utc);
                    await Calendar.UpdateEvent(
                        ScheduleUtils.GetCalendarCodeForOutputChannel(guildConfig, outputChannel.Id), @event);
                }
                else
                {
                    Log.Warning("Failed to find calendar entry for event.");
                }
#endif

                await SortEmbeds(guildConfig, Context.Guild, outputChannel);

                Log.Information("Rescheduled announcement from {OldTime} to {NewTime}",
                    curTime.UtcDateTime.ToShortTimeString(),
                    newTime.UtcDateTime.ToShortTimeString());
                await ReplyAsync("Announcement rescheduled!");
            }
            else
            {
                await ReplyAsync("No announcement by you was found at that time!");
            }
        }

        [Command("unannounce", RunMode = RunMode.Async)]
        [Description("Cancel an event. Usage: `~unannounce Time`")]
        public async Task Unannounce([Remainder] string args)
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return;

            var outputChannel = ScheduleUtils.GetOutputChannel(guildConfig, Context.Guild, Context.Channel);
            if (outputChannel == null) return;
            
            var announceChannel = ScheduleUtils.GetAnnouncementChannel(guildConfig, Context.Guild, Context.Channel);

            var username = Context.User.ToString();

            IUserMessage embedMessage;
            IUserMessage announceChannelMessage;
            IEmbed embed;
            DateTimeOffset time;

            // Check if the user entered a message ID instead of a time
            var splitArgs = args.Split();
            if (splitArgs.Length >= 1 && splitArgs[0].Length > 8 && splitArgs[0].All(char.IsDigit))
            {
                if (!ulong.TryParse(splitArgs[0], out var announceMessageId))
                {
                    await ReplyAsync("Could not read message ID!");
                    return;
                }

                var announceMessage = await Context.Channel.GetMessageAsync(announceMessageId);
                if (announceMessage == null)
                {
                    await ReplyAsync("The message with that ID does not exist in this channel!");
                    return;
                }

                (embedMessage, embed) = await FindAnnouncementById(outputChannel, Context.User, splitArgs[0].Trim());
                (announceChannelMessage, _) = await FindAnnouncementById(announceChannel, Context.User, splitArgs[0].Trim());

                time = embed.Timestamp.Value;
            }
            else
            {
                // Read time from message
                var (timeTemp, tzi) = ScheduleUtils.ParseTime(args);
                time = timeTemp;

                var isDST = tzi.IsDaylightSavingTime(time);
                if (isDST)
                    time = time.AddHours(-1);

                if (time.ToOffset(tzi.BaseUtcOffset) < DateTimeOffset.Now.ToOffset(tzi.BaseUtcOffset))
                {
                    await ReplyAsync("That time is in the past!");
                    return;
                }

                (embedMessage, embed) = await FindAnnouncement(outputChannel, Context.User, time);
                (announceChannelMessage, _) = await FindAnnouncement(announceChannel, Context.User, time);
            }

            if (embedMessage != null)
            {
                var updatedEmbed = new EmbedBuilder()
                    .WithTitle(embed.Title)
                    .WithColor(embed.Color.Value)
                    .WithDescription("❌ Cancelled")
                    .Build();
                
                await embedMessage.ModifyAsync(props =>
                {
                    props.Embeds = new[] { updatedEmbed };
                });

                if (announceChannelMessage != null)
                {
                    await announceChannelMessage.ModifyAsync(props =>
                    {
                        props.Embeds = new[] { updatedEmbed };
                    });
                }

                async void DeleteEmbed()
                {
                    await Task.Delay(1000 * 60 * 60 * 2); // 2 hours
                    try
                    {
                        await embedMessage.DeleteAsync();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Failed to delete embed message. It may have already been deleted!");
                    }
                }

                new Task(DeleteEmbed).Start();

#if DEBUG
                var @event = await FindEvent("drs", username, time);
#else
                var @event = await FindEvent(ScheduleUtils.GetCalendarCodeForOutputChannel(guildConfig, outputChannel.Id), username, time);
#endif
                if (@event != null)
                {
#if DEBUG
                    await Calendar.DeleteEvent("drs", @event.ID);
#else
                    await Calendar.DeleteEvent(ScheduleUtils.GetCalendarCodeForOutputChannel(guildConfig, outputChannel.Id), @event.ID);
#endif
                }
                else
                {
                    Log.Warning("Failed to find calendar entry for event.");
                }

                if (embed?.Footer != null)
                {
                    await Db.RemoveAllEventReactions(ulong.Parse(embed.Footer?.Text));
                }

                await ReplyAsync("Event cancelled.");
            }
            else
            {
                await ReplyAsync("No event by you was found at that time!");
            }
        }

        [Command("drsruns")]
        [Description("Lists the estimated number of runs of each type for Delubrum Reginae (Savage) right now.")]
        public async Task ListDRSRunCountsByType([Remainder] string args = "")
        {
            Log.Information(args);

            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return;

            var outputChannel = Context.Guild.GetTextChannel(guildConfig.DelubrumScheduleOutputChannel);
            var events = await ScheduleUtils.GetEvents(outputChannel);

            var eventCounts = events
                .Select(@event => @event.Item2)
                .Select(embed => embed.Description)
                .GroupBy(description =>
                {
                    foreach (var (role, roleName) in DelubrumProgressionRoles.LFGRoles)
                    {
                        if (description.Contains(role.ToString()))
                        {
                            return roleName;
                        }
                    }

                    return "Unknown";
                })
                .ToDictionary(group => group.Key, group => group.Count());

            var typeOrder = new[]
            {
                "Fresh Progression",
                "Trinity Seeker Progression",
                "Queen's Guard Progression",
                "Trinity Avowed Progression",
                "Stygimoloch Lord Progression",
                "The Queen Progression",
                "Unknown",
            };

            await ReplyAsync(typeOrder.Aggregate("Estimated run counts by type:\n```",
                                 (agg, type) => agg + $"\n{type}: {(eventCounts.ContainsKey(type) ? eventCounts[type] : 0)}") +
                             "\n```");
        }

        private async Task<(IUserMessage, IEmbed)> FindAnnouncementById(IMessageChannel channel, IPresence user, string id)
        {
            using var typing = Context.Channel.EnterTypingState();
            await foreach (var page in channel.GetMessagesAsync())
            {
                foreach (var message in page)
                {
                    var restMessage = (IUserMessage)message;

                    var embed = restMessage.Embeds.FirstOrDefault();
                    if (embed?.Footer == null) continue;

                    var embedTime = embed.Timestamp;
                    if (embedTime == null) continue;

                    if (embed.Author?.Name != user.ToString() || restMessage.Content != id) continue;

                    return (restMessage, embed);
                }
            }

            return (null, null);
        }

        private async Task<(IUserMessage, IEmbed)> FindAnnouncement(IMessageChannel channel, IPresence user, DateTimeOffset time)
        {
            using var typing = Context.Channel.EnterTypingState();

            var announcements = new List<(IUserMessage, IEmbed)>();

            await foreach (var page in channel.GetMessagesAsync())
            {
                foreach (var message in page)
                {
                    var restMessage = (IUserMessage)message;

                    var embed = restMessage.Embeds.FirstOrDefault();
                    if (embed?.Footer == null) continue;

                    var embedTime = embed.Timestamp;
                    if (embedTime == null) continue;

                    if (embed.Author?.Name != user.ToString() || embedTime.Value != time) continue;

                    announcements.Add((restMessage, embed));
                }
            }

            switch (announcements.Count)
            {
                case 0:
                    return (null, null);
                case 1:
                    return announcements[0];
                default:
                    {
                        var query = "Multiple runs at that time were found; which one would you like to select?";
                        for (var i = 0; i < announcements.Count; i++)
                        {
                            query += $"\n{i + 1}) {announcements[i].Item1.GetJumpUrl()}";
                        }
                        await ReplyAsync(query);

                        var j = -1;
                        const int stopPollingDelayMs = 250;
                        for (var i = 0; i < (5 * 60000) / stopPollingDelayMs; i++)
                        {
                            var newMessages = await Context.Channel.GetMessagesAsync(limit: 1).FlattenAsync();
                            var newMessage = newMessages.FirstOrDefault(m => m.Author.Id == Context.User.Id);
                            if (newMessage != null && int.TryParse(newMessage.Content, out j))
                            {
                                break;
                            }
                            await Task.Delay(stopPollingDelayMs);
                        }

                        if (j != -1) return announcements[j - 1];
                        await ReplyAsync("No response received; cancelling...");
                        return (null, null);
                    }
            }
        }

        private async Task<(IUserMessage, IEmbed)> FindAnnouncement(IMessageChannel channel, ulong eventId)
        {
            using var typing = Context.Channel.EnterTypingState();

            await foreach (var page in channel.GetMessagesAsync())
            {
                foreach (var message in page)
                {
                    var restMessage = (IUserMessage)message;

                    var embed = restMessage.Embeds.FirstOrDefault();
                    if (embed?.Footer == null) continue;

                    if (ulong.TryParse(embed.Footer?.Text, out var embedEId) && embedEId == eventId)
                    {
                        return (restMessage, embed);
                    }
                }
            }

            return (null, null);
        }
    }
}