namespace Prima.Application;

public static class TaskUtils
{
    /// <summary>
    /// Transforms a task-returning function into one that can't be awaited. This is intended
    /// to wrap Discord.NET's event callbacks, since they block the gateway task. Only use this
    /// on event handlers that the client logs warnings about; detaching a task from the gateway
    /// task causes it to no longer be thread-safe with respect to the Discord client.
    /// <br /><br />
    /// Further reading:
    /// https://discordnet.dev/guides/concepts/events.html
    /// </summary>
    public static Task Detach(Func<Task> fn)
    {
        Task.Run(fn);
        return Task.CompletedTask;
    } 
}