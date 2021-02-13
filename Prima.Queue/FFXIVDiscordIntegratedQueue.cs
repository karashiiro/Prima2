using System;
using System.Linq;
using Discord;
using Discord.Commands;
using Prima.Extensions;

namespace Prima.Queue
{
    public class FFXIVDiscordIntegratedQueue : FFXIV3RoleQueue
    {
        public DiscordIntegratedEnqueueResult EnqueueWithDiscordRole(ulong userId, FFXIVRole role, IRole discordRole, SocketCommandContext context, string eventId = "")
        {
            if (context.Guild == null) return DiscordIntegratedEnqueueResult.NoGuild;
            if (!context.Guild.GetUser(userId).HasRole(discordRole)) return DiscordIntegratedEnqueueResult.DoesNotHaveRole;

            var queue = GetQueue(role);
            if (queue.Any(s => s.Id == userId)) return DiscordIntegratedEnqueueResult.AlreadyInQueue;

            var slot = new QueueSlot(userId, eventId, roleIds: new[] { discordRole.Id });
            queue.Add(slot);
            return DiscordIntegratedEnqueueResult.Success;
        }

        public ulong? DequeueWithDiscordRole(FFXIVRole role, IRole discordRole, SocketCommandContext context, string eventId = "")
        {
            if (context.Guild == null) return null;
            var queue = GetQueue(role);
            if (queue.Count == 0) return null;

            ulong user;
            lock (queue)
            {
                var slot = queue.FirstOrDefault(s => SlotHasRole(s, discordRole, context) && s.EventId == eventId);

                if (slot == null)
                {
                    return null;
                }

                user = slot.Id;
            }

            RemoveAll(user);
            return user;
        }

        public int CountWithDiscordRole(FFXIVRole role, IRole discordRole, SocketCommandContext context)
        {
            return (role switch
            {
                FFXIVRole.DPS => _dpsQueue,
                FFXIVRole.Healer => _healerQueue,
                FFXIVRole.Tank => _tankQueue,
                FFXIVRole.None => GetQueue(FFXIVRole.None),
                _ => throw new NotImplementedException(),
            }).Count(s => SlotHasRole(s, discordRole, context));
        }

        public int CountDistinctWithDiscordRole(IRole discordRole, SocketCommandContext context)
        {
            return _dpsQueue
                .Concat(_healerQueue)
                .Concat(_tankQueue)
                .Where(s => SlotHasRole(s, discordRole, context))
                .Select(s => s.Id)
                .Distinct()
                .Count();
        }

        public int GetPositionWithDiscordRole(ulong userId, FFXIVRole role, IRole discordRole, SocketCommandContext context)
        {
            return (role switch
            {
                FFXIVRole.DPS => _dpsQueue,
                FFXIVRole.Healer => _healerQueue,
                FFXIVRole.Tank => _tankQueue,
                FFXIVRole.None => GetQueue(FFXIVRole.None),
                _ => throw new NotImplementedException(),
            }).Where(s => SlotHasRole(s, discordRole, context)).ToList().IndexOf(s => s.Id == userId) + 1;
        }

        private static bool SlotHasRole(QueueSlot s, IRole discordRole, SocketCommandContext context)
        {
            // User-specified roles take absolute precedence over their role list
            if (s.RoleIds.Count() != 0)
            {
                return s.RoleIds.Contains(discordRole.Id);
            }

            // Alternatively, check their role list
            var discordUser = context.Guild.GetUser(s.Id);
            if (discordUser != null && discordUser.HasRole(discordRole))
            {
                return true;
            }

            return false;
        }
    }

    public enum DiscordIntegratedEnqueueResult
    {
        Success,
        DoesNotHaveRole,
        AlreadyInQueue,
        NoGuild,
    }
}