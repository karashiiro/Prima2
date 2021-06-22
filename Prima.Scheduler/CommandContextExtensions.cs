using DSharpPlus.CommandsNext;
using System.Collections.Generic;
using System.Linq;

namespace Prima.Scheduler
{
    public static class CommandContextExtensions
    {
        public static async IAsyncEnumerable<Command> GetExecutableCommandsAsync(this CommandContext ctx)
        {
            foreach (var command in ctx.CommandsNext.RegisteredCommands.Values)
            {
                var failedExecutionChecks = await command.RunChecksAsync(ctx, true);
                if (!failedExecutionChecks.Any() && command.CanExecute(ctx))
                {
                    yield return command;
                }
            }
        }
    }
}