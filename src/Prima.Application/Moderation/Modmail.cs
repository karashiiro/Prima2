using Discord;
using Discord.WebSocket;
using Prima.Services;
using Serilog;

namespace Prima.Application.Moderation;

public static class Modmail
{
    public static async Task Handler(IDbService db, SocketMessageComponent component)
    {
        if (component.Data.CustomId != "cem-modmail") return;
        if (component.Message.Channel is not ITextChannel channel) return;

        var guildConfig = db.Guilds.FirstOrDefault(g => g.Id == channel.GuildId);
        if (guildConfig == null)
        {
            Log.Warning("Modmail: no guild configuration exists for guild {GuildName}", channel.Guild.Name);
            return;
        }

        // Create the user thread
        var member = await channel.Guild.GetUserAsync(component.User.Id);
        var threadName = string.IsNullOrEmpty(member.Nickname) ? member.ToString() : member.Nickname;
        var userThread = await channel.CreateThreadAsync(threadName, ThreadType.PrivateThread);
        var requestMessage = await userThread.SendMessageAsync("Please enter the contents of your modmail here.");
        await userThread.AddUserAsync(member);

        Log.Information("Created thread {ThreadName} for user {User}", threadName, member.ToString());

        // Create the mod thread
        if (await channel.Guild.GetTextChannelAsync(guildConfig.ReportChannel) is not SocketTextChannel reportsChannel)
        {
            Log.Warning("Modmail: reports channel is not of type SocketTextChannel");
            return;
        }

        var threadStart = await reportsChannel.SendMessageAsync($"{member.Mention} just sent a modmail!");
        IThreadChannel modThread =
            reportsChannel.Threads.FirstOrDefault(t => t.Name == threadName)
            ?? await reportsChannel.CreateThreadAsync(threadName, message: threadStart);
        await modThread.SendMessageAsync($"<@&{guildConfig.Roles["Moderator"]}>: {requestMessage.GetJumpUrl()}");
    }
}