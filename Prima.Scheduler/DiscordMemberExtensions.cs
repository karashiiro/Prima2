using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Prima.Scheduler
{
    public static class DiscordMemberExtensions
    {
        public static async Task GrantRolesAsync(this DiscordMember member, DiscordRole[] roles)
        {
            foreach (var role in roles)
            {
                await member.GrantRoleAsync(role);
            }
        }

        public static bool HasRole(this DiscordMember member, DiscordRole role)
        {
            return member.Roles.Any(r => r.Id == role.Id);
        }
    }
}