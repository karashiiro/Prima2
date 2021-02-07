using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Prima.Attributes;

namespace Prima
{
    public static class DiscordUtilities
    {
        public static async Task<string> GetFormattedCommandList(
            IServiceProvider services,
            ICommandContext ctx,
            string prefix,
            string moduleName,
            ICollection<string> except = null)
        {
            var commandManager = services.GetRequiredService<CommandService>();

            var commands = (await commandManager.GetExecutableCommandsAsync(ctx, services))
                .Where(command => command.Attributes.Any(attr => attr is DescriptionAttribute))
                .Where(command => command.Module.Name == moduleName)
                .Where(command => !except?.Contains(command.Name) ?? true);

            return commands
                .Select(c =>
                {
                    var descAttr = (DescriptionAttribute)c.Attributes.First(attr => attr is DescriptionAttribute);
                    return $"`{prefix}{c.Name}` - {descAttr.Description}\n";
                })
                .Aggregate((text, next) => text + next);
        }

        public static async Task PostImage(HttpClient http, SocketCommandContext context, string uri)
        {
            var fileName = uri.Split('/').Last();
            var fileStream = await http.GetStreamAsync(new Uri(uri));
            await context.Channel.SendFileAsync(fileStream, fileName);
        }
    }

    public static class DiscordUserExtensions
    {
        public static bool HasRole(this IUser user, ulong roleId, SocketCommandContext context)
        {
            var member = context.Guild.GetUser(user.Id);
            return member.Roles.FirstOrDefault(r => r.Id == roleId) != null;
        }

        public static bool HasRole(this IUser user, IRole role, SocketCommandContext context)
        {
            return user.HasRole(role.Id, context);
        }
    }
}