using Discord;
using Discord.Commands;
using Prima.Models;
using Prima.Services;
using System;
using System.Threading.Tasks;

namespace Prima.Stable.Modules
{
    /// <summary>
    /// Includes guild configuration commands that only guild administrators should be able to execute.
    /// </summary>
    [Name("GuildConfiguration")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class GuildConfigurationModule : ModuleBase<SocketCommandContext>
    {
        public IDbService Db { get; set; }

        [Command("configure", RunMode = RunMode.Async)]
        [Alias("config")]
        public async Task ConfigureAsync(string key, [Remainder]string value)
        {
            try
            {
                await Db.SetGuildConfigurationProperty(Context.Guild.Id, key, value);
                await ReplyAsync("Property updated. Please verify your guild configuration change.");
            }
            catch (ArgumentException e)
            {
                await ReplyAsync($"Error: {e.Message}");
            }
        }

        [Command("setupguild", RunMode = RunMode.Async)]
        public async Task SetupGuildAsync()
        {
            await Db.AddGuild(new DiscordGuildConfiguration(Context.Guild.Id));
            await ReplyAsync("Guild configuration created.");
        }

        [Command("configurerole", RunMode = RunMode.Async)]
        [Alias("configrole")]
        public async Task ConfigureRoleAsync(params string[] parameters)
        {
            await Db.ConfigureRole(Context.Guild.Id, string.Join(' ', parameters[..^1]), ulong.Parse(parameters[^1]));
            await ReplyAsync("Role registered.");
        }

        [Command("deconfigurerole", RunMode = RunMode.Async)]
        [Alias("deconfigrole")]
        public async Task DeconfigureRoleAsync(params string[] parameters)
        {
            await Db.DeconfigureRole(Context.Guild.Id, string.Join(' ', parameters[0..^1]));
            await ReplyAsync("Role deregistered.");
        }

        [Command("configureroleemote", RunMode = RunMode.Async)]
        [Alias("configroleemote", "configemote")]
        public async Task ConfigureRoleEmoteAsync(params string[] parameters)
        {
            if (!ulong.TryParse(parameters[0], out var roleId))
                await ReplyAsync("The role ID is misformatted. Please ensure that it only contains numbers.");
            await Db.ConfigureRoleEmote(Context.Guild.Id, roleId, parameters[1]);
            await ReplyAsync("Emote registered.");
        }

        [Command("deconfigureroleemote", RunMode = RunMode.Async)]
        [Alias("deconfigroleemote", "deconfigemote")]
        public async Task DeconfigureRoleEmoteAsync(string emoteId)
        {
            await Db.DeconfigureRoleEmote(Context.Guild.Id, emoteId);
            await ReplyAsync("Emote deregistered.");
        }
    }
}
