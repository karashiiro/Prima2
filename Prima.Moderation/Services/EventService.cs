using Discord;
using Discord.Net;
using Discord.WebSocket;
using Prima.Models;
using Prima.Services;
using Serilog;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Color = Discord.Color;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace Prima.Moderation.Services
{
    public class EventService
    {
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;
        private readonly WebClient _wc;

        public string LastCaughtRegex { get; private set; }

        public EventService(DbService db, DiscordSocketClient client, WebClient wc)
        {
            _db = db;
            _client = client;
            _wc = wc;

            LastCaughtRegex = string.Empty;
        }

        public async Task MessageDeleted(Cacheable<IMessage, ulong> cmessage, ISocketMessageChannel ichannel)
        {
            if (!(ichannel is SocketGuildChannel channel) || _db.Guilds.All(g => g.Id != channel.Guild.Id)) return;

            var guild = channel.Guild;

            var config = _db.Guilds.Single(g => g.Id == guild.Id);

            var deletedMessageChannel = guild.GetChannel(config.DeletedMessageChannel) as SocketTextChannel ?? throw new NullReferenceException();
            var deletedCommandChannel = guild.GetChannel(config.DeletedCommandChannel) as SocketTextChannel ?? throw new NullReferenceException();

            var imessage = await cmessage.GetOrDownloadAsync(); // Should be cached
            if (imessage == null)
            {
                await deletedMessageChannel.SendMessageAsync($"<@{_db.Config.BotMaster}>, a message was deleted without being cached first!");
                Log.Warning("Message deleted and not cached!");
                return;
            }

            var message = imessage as SocketUserMessage;

            var prefix = config.Prefix == ' ' ? _db.Config.Prefix : config.Prefix;

            // Get executor of the deletion.
            var auditLogs = await guild.GetAuditLogsAsync(10).FlattenAsync();
            IUser executor = message.Author; // If no user is listed as the executor, the executor is the author of the message.
            try
            {
                var thisLog = auditLogs
                    .FirstOrDefault(log => log.Action == ActionType.MessageDeleted && DateTime.Now - log.CreatedAt < new TimeSpan(0, 5, 0));
                executor = thisLog?.User ?? message.Author; // See above.
            }
            catch (InvalidOperationException) { }

            // Build the embed.
            var messageEmbed = new EmbedBuilder()
                .WithTitle("#" + ichannel.Name)
                .WithColor(Color.Blue)
                .WithAuthor(message.Author)
                .WithDescription(message.Content)
                .WithFooter($"Deleted by {executor}", executor.GetAvatarUrl())
                .WithCurrentTimestamp()
                .Build();

            // Send the embed.
            IMessage sentMessage;
            if (message.Author.Id == _client.CurrentUser.Id || message.Content.StartsWith(prefix))
            {
                sentMessage = await deletedCommandChannel.SendMessageAsync(embed: messageEmbed);
            }
            else
            {
                sentMessage = await deletedMessageChannel.SendMessageAsync(embed: messageEmbed);
            }

            // Attach attachments as well.
            var unsaved = string.Empty;
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
            }

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

        public async Task MessageRecieved(SocketMessage rawMessage)
        {
            if (rawMessage == null)
            {
                throw new ArgumentNullException(nameof(rawMessage));
            }

            SaveAttachments(rawMessage);

            if (!(rawMessage.Channel is SocketGuildChannel channel))
                return;

            if (_db.Guilds.All(g => g.Id != channel.Guild.Id)) return;
            var guildConfig = _db.Guilds.Single(g => g.Id == channel.Guild.Id);

            // Keep the welcome channel clean.
            if (rawMessage.Channel.Id == guildConfig.WelcomeChannel)
            {
                var guild = channel.Guild;
                var prefix = guildConfig.Prefix == ' ' ? _db.Config.Prefix : guildConfig.Prefix;
                if (!guild.GetUser(rawMessage.Author.Id).GetPermissions(channel).ManageMessages)
                {
                    if (!rawMessage.Content.StartsWith($"{prefix}i") && !rawMessage.Content.ToLower().StartsWith("i") && !rawMessage.Content.StartsWith($"{prefix}agree") && !rawMessage.Content.StartsWith($"agree"))
                    {
                        try
                        {
                            await rawMessage.DeleteAsync();
                        }
                        catch (HttpException) { }
                    }
                    else
                    {
                        try
                        {
                            await Task.Delay(10000);
                            await rawMessage.DeleteAsync();
                        }
                        catch (HttpException) { }
                    }
                }
            }

            if (!rawMessage.Content.StartsWith("~report"))
            {
                await ProcessAttachments(rawMessage, channel);
            }
            await CheckTextBlacklist(rawMessage, guildConfig);
        }

        /// <summary>
        /// Check a message against the text blacklist.
        /// </summary>
        public async Task CheckTextBlacklist(SocketMessage rawMessage, DiscordGuildConfiguration guildConfig)
        {
            foreach (var regexString in guildConfig.TextBlacklist)
            {
                var match = Regex.Match(rawMessage.Content, regexString);
                if (match.Success)
                {
                    LastCaughtRegex = regexString;
                    await rawMessage.DeleteAsync();
                }
            }
        }

        /// <summary>
        /// Save attachments to a local directory. Remember to clear out this folder periodically.
        /// </summary>
        private void SaveAttachments(SocketMessage rawMessage)
        {
            if (!rawMessage.Attachments.Any()) return;
            foreach (Attachment a in rawMessage.Attachments)
            {
                _wc.DownloadFile(new Uri(a.Url), Path.Combine(_db.Config.TempDir, a.Filename));
                Log.Information("Saved attachment {Filename}", Path.Combine(_db.Config.TempDir, a.Filename));
            }
        }

        /// <summary>
        /// Convert attachments that don't render automatically to formats that do.
        /// </summary>
        private async Task ProcessAttachments(SocketMessage rawMessage, SocketGuildChannel guildChannel)
        {
            if (!rawMessage.Attachments.Any()) return;

            foreach (Attachment attachment in rawMessage.Attachments)
            {
                string justFileName = attachment.Filename.Substring(0, attachment.Filename.LastIndexOf("."));
                if (attachment.Filename.ToLower().EndsWith(".bmp") || attachment.Filename.ToLower().EndsWith(".dib"))
                {
                    try
                    {
                        Stopwatch timer = new Stopwatch();
                        using Bitmap bitmap = new Bitmap(Path.Combine(_db.Config.TempDir, attachment.Filename));
                        bitmap.Save(Path.Combine(_db.Config.TempDir, justFileName + ".png"), ImageFormat.Png);
                        timer.Stop();
                        Log.Information("Processed BMP from {DiscordName}, ({Time}ms)!", $"{rawMessage.Author.Username}#{rawMessage.Author.Discriminator}", timer.ElapsedMilliseconds);
                        await (guildChannel as ITextChannel).SendFileAsync(Path.Combine(_db.Config.TempDir, justFileName + ".png"), $"{rawMessage.Author.Mention}: Your file has been automatically converted from BMP/DIB to PNG (BMP files don't render automatically).");
                    }
                    catch (FileNotFoundException)
                    {
                        Log.Error("Could not find file {Filename}", Path.Combine(_db.Config.TempDir, attachment.Filename));
                    }
                }
            }
        }
    }
}
