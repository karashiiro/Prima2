using System;

namespace Prima.Attributes
{
    /// <summary>
    /// Restricts usage of the attached command to a particular guild.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RestrictToGuildsAttribute : Attribute
    {
        public ulong[] GuildIds { get; set; }

        public RestrictToGuildsAttribute(params ulong[] guildIds)
        {
            GuildIds = guildIds;
        }
    }
}
