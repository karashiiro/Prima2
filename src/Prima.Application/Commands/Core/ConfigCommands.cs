using Discord;
using Discord.Commands;
using Prima.Models;
using Prima.Services;

namespace Prima.Application.Commands.Core;

[Name("Configuration")]
public class ConfigCommands : ModuleBase<SocketCommandContext>
{
    private readonly IDbService _db;

    public ConfigCommands(IDbService db)
    {
        _db = db;
    }
    
    [Command("configglobal", RunMode = RunMode.Async)]
    [RequireOwner]
    public async Task ConfigureGlobalAsync(string key, string value)
    {
        try
        {
            await _db.SetGlobalConfigurationProperty(key, value);
            await ReplyAsync("Property updated. Please verify your global configuration change.");
        }
        catch (ArgumentException e)
        {
            await ReplyAsync($"Error: {e.Message}");
        }
    }

    [Command("configure", RunMode = RunMode.Async)]
    [Alias("config")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task ConfigureAsync(string key, [Remainder] string value)
    {
        try
        {
            await _db.SetGuildConfigurationProperty(Context.Guild.Id, key, value);
            await ReplyAsync("Property updated. Please verify your guild configuration change.");
        }
        catch (ArgumentException e)
        {
            await ReplyAsync($"Error: {e.Message}");
        }
    }

    [Command("setupguild", RunMode = RunMode.Async)]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task SetupGuildAsync()
    {
        await _db.AddGuild(new DiscordGuildConfiguration(Context.Guild.Id));
        await ReplyAsync("Guild configuration created.");
    }

    [Command("configurerole", RunMode = RunMode.Async)]
    [Alias("configrole")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task ConfigureRoleAsync(params string[] parameters)
    {
        await _db.ConfigureRole(Context.Guild.Id, string.Join(' ', parameters[..^1]), ulong.Parse(parameters[^1]));
        await ReplyAsync("Role registered.");
    }

    [Command("deconfigurerole", RunMode = RunMode.Async)]
    [Alias("deconfigrole")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task DeconfigureRoleAsync(params string[] parameters)
    {
        await _db.DeconfigureRole(Context.Guild.Id, string.Join(' ', parameters[0..^1]));
        await ReplyAsync("Role deregistered.");
    }

    [Command("configureroleemote", RunMode = RunMode.Async)]
    [Alias("configroleemote", "configemote")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task ConfigureRoleEmoteAsync(params string[] parameters)
    {
        if (!ulong.TryParse(parameters[0], out var roleId))
            await ReplyAsync("The role ID is misformatted. Please ensure that it only contains numbers.");
        await _db.ConfigureRoleEmote(Context.Guild.Id, roleId, parameters[1]);
        await ReplyAsync("Emote registered.");
    }

    [Command("deconfigureroleemote", RunMode = RunMode.Async)]
    [Alias("deconfigroleemote", "deconfigemote")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task DeconfigureRoleEmoteAsync(string emoteId)
    {
        await _db.DeconfigureRoleEmote(Context.Guild.Id, emoteId);
        await ReplyAsync("Emote deregistered.");
    }
}