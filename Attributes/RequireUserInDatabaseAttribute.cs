using Discord.Commands;
using Prima.Contexts;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Prima.Attributes
{
    public class RequireUserInDatabaseAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            using (var db = new DiscordXIVUserContext())
            {
                try
                {
                    DiscordXIVUser user = db.Users
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
}