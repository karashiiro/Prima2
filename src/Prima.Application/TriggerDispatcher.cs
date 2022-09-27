using System.Reflection;
using Discord.WebSocket;
using Prima.Application.Triggers;
using Prima.Application.Triggers.Attributes;

namespace Prima.Application;

public static class TriggerDispatcher
{
    private static readonly IReadOnlyList<BaseTrigger> Triggers = Assembly.GetExecutingAssembly().DefinedTypes
        .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(BaseTrigger)))
        .Select(Activator.CreateInstance)
        .Where(t => t != null)
        .Select(t => (BaseTrigger)t!)
        .ToList();

    public static Task Handler(DiscordSocketClient client, SocketMessage message)
    {
        Task.Run(() => HandlerAsync(client, message));
        return Task.CompletedTask;
    }

    private static async Task HandlerAsync(DiscordSocketClient client, SocketMessage message)
    {
        if (message.Author.Id == client.CurrentUser.Id) return;

        SocketGuild? guild = null;
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