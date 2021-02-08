using System;
using System.Linq;
using Discord;
using Discord.Commands;
using Prima.Extensions;

namespace Prima.Queue
{
    public class FFXIVDiscordIntegratedQueue : FFXIV3RoleQueue
    {
        public ulong? DequeueWithDiscordRole(FFXIVRole role, IRole discordRole, SocketCommandContext context)
        {
            if (context.Guild == null) return null;
            var queue = GetQueue(role);
            if (queue.Count == 0) return null;

            ulong user;
            lock (queue)
            {
                // Get the first queue member with the specified role.
                var slot = queue.FirstOrDefault(s =>
                {
                    var discordUser = context.Guild.GetUser(s.Id);
                    if (discordUser != null && discordUser.HasRole(discordRole))
                    {
                        return true;
                    }

                    return false;
                });

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
            }).Where(SlotHasRole(discordRole, context)).Count();
        }

        public int CountDistinctWithDiscordRole(IRole discordRole, SocketCommandContext context)
        {
            return _dpsQueue
                .Concat(_healerQueue)
                .Concat(_tankQueue)
                .Where(SlotHasRole(discordRole, context))
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
            }).Where(SlotHasRole(discordRole, context)).ToList().IndexOf(s => s.Id == userId) + 1;
        }

        private static Func<QueueSlot, bool> SlotHasRole(IRole discordRole, SocketCommandContext context)
        {
            return s =>
            {
                var discordUser = context.Guild.GetUser(s.Id);
                if (discordUser != null && discordUser.HasRole(discordRole))
                {
                    return true;
                }

                return false;
            };
        }
}
}