using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.WebSocket;
using Prima.Stable.Handlers.Triggers;

namespace Prima.Stable.Handlers
{
    public static class TriggerDispatcher
    {
        private static readonly IEnumerable<BaseTrigger> Triggers = Assembly.GetExecutingAssembly().DefinedTypes
            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(BaseTrigger)))
            .Select(t => (BaseTrigger)Activator.CreateInstance(t))
            .Where(t => t != null)
            .ToList();

        public static async Task Handler(DiscordSocketClient client, SocketMessage message)
        {
            SocketGuild guild = null;
            if (message.Channel is SocketGuildChannel guildChannel)
            {
                guild = guildChannel.Guild;
            }
            
            var applicableTriggers = Triggers
                .Where(t => t.GetApplicableGuildId() == (guild?.Id ?? 0))
                .Where(t => t.Condition(message));

            await Task.WhenAll(applicableTriggers.Select(t => t.Execute(client, message)));
        }
    }
}
