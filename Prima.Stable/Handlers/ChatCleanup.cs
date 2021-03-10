using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Prima.Models;
using Prima.Services;
using Serilog;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace Prima.Stable.Handlers
{
    public class ChatCleanup
    {
        public static string LastCaughtRegex { get; private set; }

        public static async Task Handler(IDbService db, WebClient wc, SocketMessage rawMessage)
        {
            if (rawMessage == null)
            {
                throw new ArgumentNullException(nameof(rawMessage));
            }

            SaveAttachments(db, wc, rawMessage);

            if (!(rawMessage.Channel is SocketGuildChannel channel))
                return;

            if (db.Guilds.All(g => g.Id != channel.Guild.Id)) return;
            var guildConfig = db.Guilds.Single(g => g.Id == channel.Guild.Id);

            // Keep the welcome channel clean.
            if (rawMessage.Channel.Id == guildConfig.WelcomeChannel)
            {
                var guild = channel.Guild;
                var prefix = guildConfig.Prefix == ' ' ? db.Config.Prefix : guildConfig.Prefix;
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
                await ProcessAttachments(db, rawMessage, channel);
            }
            await CheckTextBlacklist(rawMessage, guildConfig);
        }

        /// <summary>
        /// Check a message against the text blacklist.
        /// </summary>
        private static async Task CheckTextBlacklist(SocketMessage rawMessage, DiscordGuildConfiguration guildConfig)
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
        private static void SaveAttachments(IDbService db, WebClient wc, SocketMessage rawMessage)
        {
            if (!rawMessage.Attachments.Any()) return;
            foreach (var a in rawMessage.Attachments)
            {
                wc.DownloadFile(new Uri(a.Url), Path.Combine(db.Config.TempDir, a.Filename));
                Log.Information("Saved attachment {Filename}", Path.Combine(db.Config.TempDir, a.Filename));
            }
        }

        /// <summary>
        /// Convert attachments that don't render automatically to formats that do.
        /// </summary>
        private static async Task ProcessAttachments(IDbService db, SocketMessage rawMessage, IGuildChannel guildChannel)
        {
            if (!rawMessage.Attachments.Any()) return;

            foreach (var attachment in rawMessage.Attachments)
            {
                var justFileName = attachment.Filename.Substring(0, attachment.Filename.LastIndexOf("."));
                if (attachment.Filename.ToLower().EndsWith(".bmp") || attachment.Filename.ToLower().EndsWith(".dib"))
                {
                    try
                    {
                        var timer = new Stopwatch();
                        using var bitmap = new Bitmap(Path.Combine(db.Config.TempDir, attachment.Filename));
                        bitmap.Save(Path.Combine(db.Config.TempDir, justFileName + ".png"), ImageFormat.Png);
                        timer.Stop();
                        Log.Information("Processed BMP from {DiscordName}, ({Time}ms)!", $"{rawMessage.Author.Username}#{rawMessage.Author.Discriminator}", timer.ElapsedMilliseconds);
                        await (guildChannel as ITextChannel).SendFileAsync(Path.Combine(db.Config.TempDir, justFileName + ".png"), $"{rawMessage.Author.Mention}: Your file has been automatically converted from BMP/DIB to PNG (BMP files don't render automatically).");
                    }
                    catch (FileNotFoundException)
                    {
                        Log.Error("Could not find file {Filename}", Path.Combine(db.Config.TempDir, attachment.Filename));
                    }
                }
            }
        }
    }
}