using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Prima.DiscordNet.Attributes
{
    /// <summary>
    /// Prevents a particular command from being used in certain guilds.
    /// </summary>
    public class RestrictFromGuildsAttribute : PreconditionAttribute
    {
        public IEnumerable<ulong> GuildIds { get; }

        public RestrictFromGuildsAttribute(params ulong[] guildIds)
        {
            GuildIds = guildIds;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (!(context.User is IGuildUser guildUser)) // Not in a guild to begin with
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            return Task.FromResult(!GuildIds.Contains(guildUser.GuildId)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("This guild does not support usage of this command."));
        }
    }
}
