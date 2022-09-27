using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Rest;
using Prima.DiscordNet.Attributes;

namespace Prima.DiscordNet
{
    public static class DiscordUtilities
    {
        public static string GetFormattedCommandList(
            Type module,
            string prefix,
            ICollection<string> except = null)
        {
            var commands = module.GetMethods()
                .Where(command => command.GetCustomAttribute<DescriptionAttribute>() != null)
                .Where(command => !except?.Contains(command.Name) ?? true);

            return commands
                .Select(c =>
                {
                    var commandAttr = c.GetCustomAttribute<CommandAttribute>();
                    var descAttr = c.GetCustomAttribute<DescriptionAttribute>();
                    return $"`{prefix}{commandAttr?.Text}` - {descAttr?.Description}\n";
                })
                .Aggregate((text, next) => text + next);
        }

        public static async Task PostImage(HttpClient http, SocketCommandContext context, string uri)
        {
            await context.Channel.SendMessageAsync(uri);
        }

        public static async Task<IUser> GetUserFromMention(string userMention, ICommandContext context)
        {
            var id = ulong.Parse(string.Join("", userMention.Where(char.IsDigit).ToArray()));
            return await context.Client.GetUserAsync(id);
        }

        public static async Task<IGuildUser?> GetGuildUser(DiscordSocketClient client, SocketGuild guild, string name)
        {
            await client.DownloadUsersAsync(new[] { guild });
            return guild.Users
                .FirstOrDefault(u => u.Nickname == name || u.ToString() == name || GetCleanUsername(u) == name);
        }

        public static string GetCleanUsername(IUser user)
        {
            return $"{user.Username.Trim()}#{user.Discriminator}"; // Is this a bug in Discord.NET?
        }
    }

    public static class DiscordUserExtensions
    {
        public static bool MemberHasRole(this IGuildUser member, ulong roleId, ICommandContext context)
        {
            return member.RoleIds.FirstOrDefault(rId => rId == roleId) != default;
        }

        public static bool MemberHasRole(this IGuildUser member, IRole role, ICommandContext context)
        {
            return member.MemberHasRole(role.Id, context);
        }

        public static bool HasRole(this IUser user, ulong roleId, SocketCommandContext context)
        {
            var member = context.Guild.GetUser(user.Id);
            return member.Roles.FirstOrDefault(r => r.Id == roleId) != null;
        }

        public static bool HasRole(this IUser user, IRole role, SocketCommandContext context)
        {
            return user.HasRole(role.Id, context);
        }

        public static bool HasRole(this IGuildUser member, ulong roleId)
        {
            return member.RoleIds.Contains(roleId);
        }

        public static bool HasRole(this IGuildUser member, IRole role)
        {
            return member.HasRole(role?.Id ?? 0);
        }

        public static bool HasRole(this RestGuildUser member, ulong roleId)
        {
            return member.RoleIds.FirstOrDefault(r => r == roleId) != default;
        }

        public static bool HasRole(this RestGuildUser member, IRole role)
        {
            return member.HasRole(role?.Id ?? 0);
        }
    }
}