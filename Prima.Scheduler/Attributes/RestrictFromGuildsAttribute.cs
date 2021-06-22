using System;
using System.Collections.Generic;

namespace Prima.Scheduler.Attributes
{
    /// <summary>
    /// Prevents a particular command from being used in certain guilds.
    /// </summary>
    public class RestrictFromGuildsAttribute : Attribute
    {
        public IEnumerable<ulong> GuildIds { get; }

        public RestrictFromGuildsAttribute(params ulong[] guildIds)
        {
            GuildIds = guildIds;
        }
    }
}
