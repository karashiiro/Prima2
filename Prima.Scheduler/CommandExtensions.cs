using System.Linq;
using DSharpPlus.CommandsNext;
using Prima.Scheduler.Attributes;

namespace Prima.Scheduler
{
    public static class CommandExtensions
    {
        public static bool CanExecute(this Command command, CommandContext ctx)
        {
            var guild = ctx.Guild;
            var channel = ctx.Channel;

            var restrictedToAttr = (RestrictToGuildsAttribute)command.CustomAttributes.FirstOrDefault(attr => attr is RestrictToGuildsAttribute);
            if (restrictedToAttr != null && (guild == null || restrictedToAttr.GuildIds.Contains(guild.Id)))
                return false;

            var restrictedFromAttr = (RestrictFromGuildsAttribute)command.CustomAttributes.FirstOrDefault(attr => attr is RestrictFromGuildsAttribute);
            if (restrictedFromAttr != null && (guild != null && restrictedFromAttr.GuildIds.Contains(guild.Id)))
                return false;

            var disabledInChannelsAttr = (DisableInChannelsForGuildAttribute)command.CustomAttributes.FirstOrDefault(attr => attr is DisableInChannelsForGuildAttribute);
            if (guild != null && guild.Id == disabledInChannelsAttr?.Guild)
            {
                if (disabledInChannelsAttr.ChannelIds.Contains(channel.Id))
                {
                    return false;
                }
            }

            return true;
        }
    }
}