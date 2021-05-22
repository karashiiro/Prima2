using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Prima.Attributes
{
    public class CEMRequireRoleOrMentorPlusAttribute : PreconditionAttribute
    {
        private const ulong CEMMentorRoleId = 579916868035411968;

        private readonly ulong _roleId;

        public CEMRequireRoleOrMentorPlusAttribute(ulong roleId)
        {
            _roleId = roleId;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.User is not IGuildUser member)
            {
                return Task.FromResult(PreconditionResult.FromError("Command cannot be executed outside of a guild."));
            }

            var role = member.Guild.GetRole(_roleId);

            if (member.MemberHasRole(role, context) || member.MemberHasRole(CEMMentorRoleId, context) || member.GuildPermissions.KickMembers)
                return Task.FromResult(PreconditionResult.FromSuccess());

            _ = Task.Run(async () =>
            {
                var res = await context.Channel.SendMessageAsync(
                    $"{member.Mention}, you don't have the {role.Name} role!");
                await Task.Delay(5000);
                await res.DeleteAsync();
            });

            return Task.FromResult(PreconditionResult.FromError($"User does not have required role \"{role.Name}\" or Mentor+."));
        }
    }
}