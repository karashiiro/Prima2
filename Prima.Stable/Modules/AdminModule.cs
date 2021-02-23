using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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

        [Command("setrolecolor")]
        public async Task SetRoleColor(ulong roleId, string hexCode)
        {
            var regex = new Regex(@"[0-9a-fA-F]{6}");
            var justHex = regex.Match(hexCode).Value;
            var red = byte.Parse(justHex[..2], NumberStyles.HexNumber);
            var green = byte.Parse(justHex[2..4], NumberStyles.HexNumber);
            var blue = byte.Parse(justHex[4..], NumberStyles.HexNumber);

            var role = Context.Guild.GetRole(roleId);
            await role.ModifyAsync(props =>
            {
                props.Color = new Color(red, green, blue);
            });

            await ReplyAsync("Role color updated!");
        }
    }
}