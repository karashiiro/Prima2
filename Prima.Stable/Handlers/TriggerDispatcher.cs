using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.WebSocket;
using Prima.Stable.Handlers.Triggers;
using Prima.Stable.Handlers.Triggers.Attributes;
using Serilog;

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
            if (message.Author.Id == client.CurrentUser.Id) return;

            SocketGuild guild = null;
            if (message.Channel is SocketGuildChannel guildChannel)
            {
                guild = guildChannel.Guild;
            }
            
            var applicableTriggers = Triggers
                .Where(t => t.IsGuildApplicable(guild))
                .Where(t => t.Condition(message))
                .ToList();

            foreach (var trigger in applicableTriggers.Where(ShouldRunFirst))
            {
                await trigger.Execute(client, message);
            }

            await Task.WhenAll(applicableTriggers
                .Where(t => !ShouldRunFirst(t))
                .Select(t => t.Execute(client, message)));
        }

        private static bool ShouldRunFirst(BaseTrigger trigger)
        {
            var execute = (Func<DiscordSocketClient, SocketMessage, Task>)trigger.Execute;
            return execute.Method.GetCustomAttribute<ShouldRunFirstAttribute>() != null;
        }
    }
}
