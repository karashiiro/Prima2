using Discord;
using Discord.WebSocket;
using Prima.Models;
using Prima.Services;
using Serilog;
using Color = Discord.Color;

namespace Prima.Application.Moderation;

public class AuditDeletion
{
    public static async Task Handler(IDbService db, DiscordSocketClient client, Cacheable<IMessage, ulong> cmessage, Cacheable<IMessageChannel, ulong> cchannel)
    {
        var ichannel = await cchannel.GetOrDownloadAsync();

        if (ichannel is not SocketGuildChannel channel || db.Guilds.All(g => g.Id != channel.Guild.Id)) return;

        var guild = channel.Guild;

        var config = db.Guilds.Single(g => g.Id == guild.Id);

        CachedMessage? cachedMessage;
        var imessage = await cmessage.GetOrDownloadAsync();
        if (imessage == null)
        {
            cachedMessage = db.CachedMessages.FirstOrDefault(m => m.MessageId == cmessage.Id);
            if (cachedMessage == null)
            {
                Log.Warning("Message deleted and not cached; this probably happened in #welcome");
                return;
            }
        }
        else
        {
            cachedMessage = new CachedMessage
            {
                AuthorId = imessage.Author.Id,
                ChannelId = imessage.Channel.Id,
                Content = imessage.Content,
                MessageId = cmessage.Id,
                UnixMs = imessage.Timestamp.ToUnixTimeMilliseconds(),
            };
        }

        var prefix = config.Prefix == ' ' ? db.Config.Prefix : config.Prefix;

        // Get executor of the deletion.
        await Task.Delay(5000); // Wait a bit to increase the chance that Discord will emit a log in time
        var auditLogs = await guild.GetAuditLogsAsync(10).FlattenAsync();
        var author = client.GetUser(cachedMessage.AuthorId);
        IUser executor = author; // If no user is listed as the executor, the executor is the author of the message.
        try
        {
            var thisLog = auditLogs
                .FirstOrDefault(log => log.Action == ActionType.MessageDeleted && DateTime.Now - log.CreatedAt < new TimeSpan(0, 1, 30));
            executor = thisLog?.User ?? executor; // See above.
        }
        catch (InvalidOperationException) { }

        // Build the embed.
        var messageEmbed = new EmbedBuilder()
            .WithTitle("#" + ichannel.Name)
            .WithColor(Color.Blue)
            .WithAuthor(author)
            .WithDescription(cachedMessage.Content)
            .WithFooter($"Deleted by {executor}", executor.GetAvatarUrl())
            .WithCurrentTimestamp()
            .Build();

        // Send the embed.
        if (author.Id == client.CurrentUser.Id || cachedMessage.Content.StartsWith(prefix))
        {
            if (guild.GetChannel(config.DeletedCommandChannel) is SocketTextChannel deletedCommandChannel)
            {
                await deletedCommandChannel.SendMessageAsync(embed: messageEmbed);
            }
            else
            {
                Log.Warning("Deleted command channel not set!");
            }
        }
        else
        {
            if (guild.GetChannel(config.DeletedMessageChannel) is SocketTextChannel deletedMessageChannel)
            {
                await deletedMessageChannel.SendMessageAsync(embed: messageEmbed);
            }
            else
            {
                Log.Warning("Deleted message channel not set!");
            }
        }

        // TODO Attach attachments as well.
        /*var unsaved = string.Empty;
        foreach (var attachment in message.Attachments)
        {
            try
            {
                await deletedMessageChannel.SendFileAsync(Path.Combine(_db.Config.TempDir, attachment.Filename), attachment.Filename);
            }
            catch (HttpException)
            {
                unsaved += $"\n{attachment.Url}";
            }
        }
        if (!string.IsNullOrEmpty(unsaved))
        {
            await deletedMessageChannel.SendMessageAsync(Properties.Resources.UnsavedMessageAttachmentsWarning + unsaved);
        }*/

        // Copy reactions and send those, too.
        /*
        if (message.Reactions.Count > 0)
        {
            string userString = string.Empty;
            foreach (var reactionEntry in message.Reactions)
            {
                var emote = reactionEntry.Key;
                userString += $"\nUsers who reacted with {emote}:";
                IEnumerable<IUser> users = await message.GetReactionUsersAsync(emote, int.MaxValue).FlattenAsync();
                foreach (IUser user in users)
                {
                    userString += "\n" + user.Mention;
                }
            }
            await deletedMessageChannel.SendMessageAsync(userString);
        }*/
    }
}