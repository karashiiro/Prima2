using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Prima.DiscordNet.Attributes
{
    /// <summary>
    /// Restricts usage of the attached command to certain guilds.
    /// </summary>
    public class RestrictToGuildsAttribute : PreconditionAttribute
    {
        public IEnumerable<ulong> GuildIds { get; }

        public RestrictToGuildsAttribute(params ulong[] guildIds)
        {
            GuildIds = guildIds;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (!(context.User is IGuildUser guildUser))
            {
                return Task.FromResult(PreconditionResult.FromError("This command cannot be executed outside of a guild."));
            }

            return Task.FromResult(GuildIds.Contains(guildUser.GuildId)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("This guild does not support usage of this command.\n" +
                                               $"Command guilds: {GuildIds.Aggregate("", (agg, next) => agg + " " + next)}\n" +
                                               $"Target guild: {guildUser.GuildId}"));
        }
    }
}
