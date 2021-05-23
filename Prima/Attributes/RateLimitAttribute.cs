using System;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Prima.Services;

namespace Prima.Attributes
{
    public class RateLimitAttribute : PreconditionAttribute
    {
        public int TimeSeconds { get; set; }

        public bool Global { get; set; }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var rateLimits = services.GetRequiredService<RateLimitService>();
            if (!rateLimits.IsReady(command))
            {
                _ = Task.Run(async () =>
                {
                    var res = await context.Channel.SendMessageAsync(
                        $"That command cannot be used for another {rateLimits.TimeUntilReady(command)} seconds.");
                    await Task.Delay(5000);
                    await res.DeleteAsync();
                });
                return PreconditionResult.FromError("Command rate limit has not yet expired.");
            }

            rateLimits.ResetTime(command, TimeSeconds);
            return PreconditionResult.FromSuccess();
        }
    }
}