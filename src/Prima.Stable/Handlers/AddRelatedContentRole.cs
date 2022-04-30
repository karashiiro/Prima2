﻿using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prima.DiscordNet;
using Prima.Resources;

namespace Prima.Stable.Handlers
{
    internal static class AddRelatedContentRole
    {
        /// <summary>
        /// Check for missing content roles that a person probably wants, and add them.
        /// </summary>
        public static async Task Handler(SocketGuildUser newMember)
        {
            if (newMember.Guild.Id != SpecialGuilds.CrystalExploratoryMissions) return;

            // This is incredibly hacky just to get the job done, don't judge
            var memberRoles = newMember.Roles.Select(r => r.Name.ToLowerInvariant()).ToList();
            
            // Eureka
            if (memberRoles.Any(r =>
                {
                    if (r.Contains("lfg") || r.Contains(" nm"))
                    {
                        return r.Contains("anemos") || r.Contains("pagos") || r.Contains("pyros") || r.Contains("hydatos") || r.Contains(" ba ") || r.Contains("ozma");
                    }

                    if (r.Contains("farms"))
                    {
                        return r.Contains("bunny") || r.Contains("lockbox") || r.Contains("light");
                    }

                    return r.StartsWith("ba ");
                }))
            {
                var contentRole = newMember.Guild.GetRole(588913087818498070);
                if (!newMember.HasRole(contentRole))
                {
                    await newMember.AddRoleAsync(contentRole);
                }
            }

            // Bozja
            if (memberRoles.Any(r =>
                {
                    if (r.Contains("lfg"))
                    {
                        return r.Contains("bozja") || r.Contains("zadnor") || r.Contains("castrum") || r.Contains("dalriada") || r.Contains("delubrum") || r.Contains("drs");
                    }

                    if (r.Contains("drs"))
                    {
                        return r.Contains("willing to lead") || r.Contains("duelist") || r.Contains("caller");
                    }

                    return r == "delubrum (normal) willing to lead";
                }))
            {
                var contentRole = newMember.Guild.GetRole(588913532410527754);
                if (!newMember.HasRole(contentRole))
                {
                    await newMember.AddRoleAsync(contentRole);
                }
            }

            // ???
            if (memberRoles.Any(r => r.Contains("???")))
            {
                var contentRole = newMember.Guild.GetRole(933531316845170708);
                if (!newMember.HasRole(contentRole))
                {
                    await newMember.AddRoleAsync(contentRole);
                }
            }
        }
    }
}
