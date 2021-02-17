using Discord;
using Discord.WebSocket;
using Prima.Models;
using Prima.Queue.Resources;
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

        public static async Task HandlerAdd(DiscordSocketClient client, FFXIV3RoleQueueService queueService, DbService db, Cacheable<IUserMessage, ulong> cachedMessage, SocketReaction reaction)
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

            var messagedOnce = false;
            var user = client.GetUser(userId);
            var scheduleQueues = GetScheduleQueues(guildConfig, reaction.Channel.Id);
            if (scheduleQueues == null) return;
            foreach (var queueChannelId in scheduleQueues)
            {
                var queueName = QueueInfo.LfgChannels[queueChannelId];
                var queue = queueService.GetOrCreateQueue(queueName);

                if (queue.Enqueue(userId, role, eventId.Value.ToString()))
                {
                    if (!messagedOnce)
                    {
                        await user.SendMessageAsync($"You have been added to the queue for event `{eventId}`. " +
                            $"You can check your position in queue with `~queue {eventId}` in the queue channel.");
                        messagedOnce = true;
                    }
                    Log.Information("User {User} has been added to the queue {QueueName}, with event {Event}", user.ToString(), queueName, eventId);
                }
                else
                {
                    await user.SendMessageAsync("You are already in that queue, in position " +
                        $"{queue.GetPosition(userId, role, eventId.Value.ToString())}/{queue.Count(role, eventId.Value.ToString())}.\n" +
                        "If you would like to leave the queue, please use `~leavequeue` in the queue channel.");
                }
            }

            var message = await cachedMessage.GetOrDownloadAsync();
            try
            {
                await message.RemoveReactionAsync(reaction.Emote, userId);
            }
            catch { /* ignored */ }
        }

        private static IEnumerable<ulong> GetScheduleQueues(DiscordGuildConfiguration guildConfig, ulong channelId)
        {
#if DEBUG
            return new ulong[] { 766712049316265985 };
#else
            if (channelId == guildConfig.CastrumScheduleOutputChannel)
                return new ulong[] { 765994301850779709 };
            else if (channelId == guildConfig.DelubrumNormalScheduleOutputChannel)
                return new ulong[] { 806957742056013895 };
            else if (channelId == guildConfig.DelubrumScheduleOutputChannel)
                return new ulong[] { 803636739343908894, 809241125373739058 };
            return null;
#endif
        }
    }
}
