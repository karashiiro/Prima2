using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace Prima.DiscordNet.Attributes
{
    public class DisableInChannelsForGuildAttribute : PreconditionAttribute
    {
        public ulong GuildId { get; set; }

        private readonly ulong[] _channelIds;

        public DisableInChannelsForGuildAttribute(params ulong[] channelIds)
        {
            _channelIds = channelIds;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Channel is not IGuildChannel guildChannel || guildChannel.GuildId != GuildId)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            if (!_channelIds.Contains(guildChannel.Id))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            _ = Task.Run(async () =>
            {
                await context.Message.DeleteAsync();
                var reply = await context.Channel.SendMessageAsync("That command is disabled in this channel.");
                await Task.Delay(10000).ConfigureAwait(false);
                await reply.DeleteAsync();
            });

            return Task.FromResult(PreconditionResult.FromError("Command may not be executed in this channel."));

        }
    }
}