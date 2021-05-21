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
        public IDbService Db { get; set; }

        [Command("sendmessage")]
        public Task SudoMessage(ITextChannel channel, [Remainder] string message)
            => channel.SendMessageAsync(message);

        [Command("clearbrokenusers")]
        public async Task ClearBrokenUsers()
        {
            await Db.RemoveBrokenUsers();
            await ReplyAsync("Done!");
        }

        [Command("drsupgraderoles", RunMode = RunMode.Async)]
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
                .Where(u => u.HasRole(role))
                .ToList();

            var left = applicableUsers.Count;
            var pogRess = await ReplyAsync($"Downloaded full user list, upgrading {left} users...");
            foreach (var user in applicableUsers)
            {
                foreach (var cr in contingentRoles)
                {
                    if (!user.HasRole(cr))
                    {
                        await user.AddRoleAsync(cr);
                    }
                }

                left--;
                await pogRess.ModifyAsync(props => props.Content = $"Downloaded full user list, upgrading {left} users...");
            }

            await pogRess.ModifyAsync(props => props.Content = "Roles updated!");
        }
    }
}
