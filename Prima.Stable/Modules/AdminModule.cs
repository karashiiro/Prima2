using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Prima.Stable.Modules
{
    [Name("Admin")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(ChannelPermission.ManageRoles)]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("createrole")]
        public async Task CreateRole([Remainder] string name)
        {
            var nameLower = name.ToLowerInvariant();
            var existing = Context.Guild.Roles
                .Select(r => r.Name.ToLowerInvariant())
                .FirstOrDefault(r => r == nameLower);
            if (existing != null)
            {
                await ReplyAsync("A role with that name already exists!");
                return;
            }

            var newRole = await Context.Guild.CreateRoleAsync(name, GuildPermissions.None, isMentionable: false);
            await ReplyAsync("Role created!\n" +
                             "```\n" +
                             $"Role name: {newRole.Name}\n" +
                             $"Role ID: {newRole.Id}" +
                             "```");
        }
    }
}