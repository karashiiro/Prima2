using System;

namespace Prima.Attributes
{
    public class RestrictFromGuildsAttribute : Attribute
    {
        public ulong[] GuildIds { get; set; }

        public RestrictFromGuildsAttribute(params ulong[] guildIds)
        {
            GuildIds = guildIds;
        }
    }
}
