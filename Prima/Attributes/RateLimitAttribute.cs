using System;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Prima.Extensions;
using Prima.Services;

namespace Prima.Attributes
{
    public class RateLimitAttribute : PreconditionAttribute
    {
        public int TimeSeconds { get; set; }

        public bool Global { get; set; }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (!command.HasTimeout())
                return Task.FromResult(PreconditionResult.FromSuccess());

            var rateLimits = services.GetRequiredService<RateLimitService>();
            if (!rateLimits.IsReady(command))
                return Task.FromResult(PreconditionResult.FromError("Command rate limit has not yet expired."));

            rateLimits.ResetTime(command);
            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}