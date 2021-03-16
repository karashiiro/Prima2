using System;
using Discord;
using Discord.WebSocket;
using Prima.Models;
using Prima.Queue.Services;
using Prima.Resources;
using Prima.Services;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prima.Queue.Handlers
{
    public static class AnnounceReact
    {
        private static readonly IDictionary<string, FFXIVRole> RoleReactions = new Dictionary<string, FFXIVRole>
        {
            { "dps", FFXIVRole.DPS },
            { "healer", FFXIVRole.Healer },
            { "tank", FFXIVRole.Tank },
        };

        public static async Task HandlerAdd(DiscordSocketClient client, FFXIV3RoleQueueService queueService, IDbService db, Cacheable<IUserMessage, ulong> cachedMessage, SocketReaction reaction)
        {
            var userId = reaction.UserId;
            if (client.CurrentUser.Id == userId || !RoleReactions.ContainsKey(reaction.Emote.Name)) return;

            var role = RoleReactions[reaction.Emote.Name];
            
            var eventId = await AnnounceUtil.GetEventId(cachedMessage);
            if (eventId == null) return;

            var guildConfig = db.Guilds.FirstOrDefault(g => g.Id == SpecialGuilds.CrystalExploratoryMissions);
            if (guildConfig == null)
            {
                Log.Error("No guild configuration found for the default guild!");
                return;
            }
            
            var user = client.GetUser(userId);
            var guild = client.GetGuild(SpecialGuilds.CrystalExploratoryMissions);

            var message = await cachedMessage.GetOrDownloadAsync();
            var inputChannel = guild.GetTextChannel(GetScheduleInputChannel(guildConfig, message.Channel.Id));
            if (inputChannel == null) return;
            var eventMessage = await inputChannel.GetMessageAsync(eventId.Value);

            var host = guild.GetUser(eventMessage.Author.Id);
            var discordRoles = DelubrumProgressionRoles.Roles.Keys
                .Select(rId => guild.GetRole(rId));
            var authorHasProgressionRole = discordRoles.Any(dr => host.HasRole(dr));
            var freshProg = !authorHasProgressionRole || eventMessage.Content.ToLowerInvariant().Contains("810201516291653643");
#if DEBUG
            Log.Information("Fresh prog: {FreshProg}", freshProg);
#endif

            var scheduleQueue = GetScheduleQueue(guildConfig, freshProg, reaction.Channel.Id);
            if (scheduleQueue == 0) return;

            var queueName = QueueInfo.LfgChannels[scheduleQueue];
            var queue = queueService.GetOrCreateQueue(queueName);
            
            if (queue.Enqueue(userId, role, eventId.Value.ToString()))
            {
                var embed = message.Embeds.FirstOrDefault(e => e.Type == EmbedType.Rich);
                var eventTime = embed?.Timestamp;
                if (eventTime != null && eventTime.Value.AddHours(-2) <= DateTimeOffset.Now)
                {
                    queue.ConfirmEvent(userId, eventId.Value.ToString());
#if DEBUG
                    Log.Information("Auto-confirmed.");
#endif
                }
#if DEBUG
                else
                {
                    if (eventTime != null)
                    {
                        Log.Information((eventTime.Value - DateTimeOffset.Now).ToString());
                    }

                    Log.Information("Not auto-confirmed.");
                }
#endif

                await user.SendMessageAsync($"You have been added to the {role} queue for event `{eventId}`. " +
                                            "You can check your position in queue with `~queue` in the queue channel.");
                Log.Information("User {User} has been added to the {FFXIVRole} queue for {QueueName}, with event {Event}", user.ToString(), role.ToString(), queueName, eventId);
            }
            else
            {
                await user.SendMessageAsync("You are already in that queue, in position " +
                                            $"{queue.GetPosition(userId, role, eventId.Value.ToString())}/{queue.Count(role, eventId.Value.ToString())}.\n" +
                                            $"If you would like to leave the queue, please use `~leavequeue {eventId}` in the queue channel.");
            }

            queueService.Save();

            try
            {
                await message.RemoveReactionAsync(reaction.Emote, userId);
            }
            catch { /* ignored */ }
        }

        private static ulong GetScheduleInputChannel(DiscordGuildConfiguration guildConfig, ulong channelId)
        {
            if (channelId == guildConfig.CastrumScheduleOutputChannel)
                return guildConfig.CastrumScheduleInputChannel;
            else if (channelId == guildConfig.DelubrumNormalScheduleOutputChannel)
                return guildConfig.DelubrumNormalScheduleInputChannel;
            else if (channelId == guildConfig.DelubrumScheduleOutputChannel)
                return guildConfig.DelubrumScheduleInputChannel;
            else if (channelId == guildConfig.ScheduleOutputChannel)
                return guildConfig.ScheduleInputChannel;
            return 0;
        }

        private static ulong GetScheduleQueue(DiscordGuildConfiguration guildConfig, bool freshProg, ulong channelId)
        {
#if DEBUG
            return 766712049316265985;
#else
            if (channelId == guildConfig.CastrumScheduleOutputChannel)
                return 765994301850779709;
            else if (channelId == guildConfig.DelubrumNormalScheduleOutputChannel)
                return 806957742056013895;
            else if (channelId == guildConfig.DelubrumScheduleOutputChannel)
            {
                if (freshProg)
                    return 809241125373739058;
                return 803636739343908894;
            }
            return 0;
#endif
        }
    }
}
