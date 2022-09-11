using Discord;
using Discord.Commands;
using Prima.DiscordNet.Attributes;
using Prima.Services;

namespace Prima.Application.Commands.Core;

[Name("Channel Information")]
public class ChannelInfoCommands : ModuleBase<SocketCommandContext>
{
    private readonly IDbService _db;

    public ChannelInfoCommands(IDbService db)
    {
        _db = db;
    }
    
    [Command("setdescription")]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public async Task SetDescriptionAsync([Remainder] string description)
    {
        await _db.DeleteChannelDescription(Context.Channel.Id);
        await _db.AddChannelDescription(Context.Channel.Id, description);
        await ReplyAsync($"{Context.User.Mention}, the help message has been updated!");
    }

    [Command("whatisthis")]
    [Description("Explains what the channel you use it in is for, if such information is available.")]
    public Task WhatIsThisAsync()
    {
        var cd = _db.ChannelDescriptions.FirstOrDefault(cd => cd.ChannelId == Context.Channel.Id);
        if (cd == null) return Task.CompletedTask;
        var embed = new EmbedBuilder()
            .WithTitle($"#{Context.Channel.Name}")
            .WithColor(new Color(0x00, 0x80, 0xFF))
            .WithThumbnailUrl("http://www.newdesignfile.com/postpic/2016/05/windows-8-help-icon_398417.png")
            .WithDescription(cd.Description)
            .Build();
        return ReplyAsync(embed: embed);
    }
}