using System;
using System.Collections.Generic;

namespace Prima.Scheduler.Attributes
{
    /// <summary>
    /// Restricts usage of the attached command to certain guilds.
    /// </summary>
    public class RestrictToGuildsAttribute : Attribute
    {
        public IEnumerable<ulong> GuildIds { get; }

        public RestrictToGuildsAttribute(params ulong[] guildIds)
        {
            GuildIds = guildIds;
        }
    }
}
