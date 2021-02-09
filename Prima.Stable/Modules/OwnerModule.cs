using System.Linq;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using Prima.Resources;
using Prima.Services;

namespace Prima.Stable.Modules
{
    [Name("Owner")]
    [RequireOwner]
    public class OwnerModule : ModuleBase<SocketCommandContext>
    {
        [Command("sendmessage")]
        public Task SudoMessage(ITextChannel channel, [Remainder] string message)
            => channel.SendMessageAsync(message);

        [Command("sancheckdrsur")]
        [RequireContext(ContextType.Guild)]
        public async Task SanCheckDRSUR([Remainder] string roleName)
        {
            var role = Context.Guild.Roles.FirstOrDefault(r => r.Name == roleName);
            if (role == null)
            {
                await ReplyAsync("No role by that name exists!");
                return;
            }

            if (!DelubrumProgressionRoles.Roles.ContainsKey(role.Id))
            {
                await ReplyAsync("Not a valid DRS role!");
                return;
            }

            var contingentRoles = DelubrumProgressionRoles.GetContingentRoles(role.Id)
                .Select(cr => Context.Guild.GetRole(cr))
                .Aggregate("", (s, socketRole) => s + socketRole.Name + "\n");

            await ReplyAsync(contingentRoles);
        }

        [Command("drsupgraderoles")]
        [RequireContext(ContextType.Guild)]
        public async Task DRSUpgradeRoles([Remainder] string roleName)
        {
            var role = Context.Guild.Roles.FirstOrDefault(r => r.Name == roleName);
            if (role == null)
            {
                await ReplyAsync("No role by that name exists!");
                return;
            }

            if (!DelubrumProgressionRoles.Roles.ContainsKey(role.Id))
            {
                await ReplyAsync("Not a valid DRS role!");
                return;
            }

            var contingentRoles = DelubrumProgressionRoles.GetContingentRoles(role.Id)
                .Select(cr => Context.Guild.GetRole(cr))
                .ToList();

            await Context.Guild.DownloadUsersAsync();
            var applicableUsers = Context.Guild.Users
                .Where(u => u.HasRole(role));
            foreach (var user in applicableUsers)
                await user.AddRolesAsync(contingentRoles);

            await ReplyAsync("Roles updated!");
        }
    }
}
