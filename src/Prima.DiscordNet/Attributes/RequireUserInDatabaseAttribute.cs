using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Prima.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Prima.DiscordNet.Attributes
{
    public class RequireUserInDatabaseAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var db = services.GetRequiredService<IDbService>();
            try
            {
                _ = db.Users
                    .Single(user => user.DiscordId == context.User.Id);
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            catch (InvalidOperationException)
            {
                return Task.FromResult(PreconditionResult.FromError(Properties.Resources.UserNotFoundError));
            }
        }
    }
}