using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Prima.DiscordNet.Extensions;
using Prima.Models;
using Prima.Services;
using Serilog;
using SixLabors.ImageSharp;
using Color = Discord.Color;

namespace Prima.DiscordNet.Handlers
{
    public class ChatCleanup
    {
        public static string LastCaughtRegex { get; private set; }

        public static async Task Handler(IDbService db, WebClient wc, ITemplateProvider templates,
            SocketMessage rawMessage)
        {
            if (rawMessage == null)
            {
                throw new ArgumentNullException(nameof(rawMessage));
            }

            if (rawMessage.Author.IsBot) return;

            SaveAttachments(db, wc, rawMessage);

            if (rawMessage.Channel is not SocketGuildChannel channel)
                return;

            if (db.Guilds.All(g => g.Id != channel.Guild.Id)) return;
            var guildConfig = db.Guilds.Single(g => g.Id == channel.Guild.Id);

            var guild = channel.Guild;

            // Keep the welcome channel clean.
            if (rawMessage.Channel.Id == guildConfig.WelcomeChannel)
            {
                var prefix = guildConfig.Prefix == ' ' ? db.Config.Prefix : guildConfig.Prefix;
                if (!guild.GetUser(rawMessage.Author.Id).GetPermissions(channel).ManageMessages)
                {
                    if (!rawMessage.Content.StartsWith($"{prefix}i") && !rawMessage.Content.ToLower().StartsWith("i") &&
                        !rawMessage.Content.StartsWith($"{prefix}agree") && !rawMessage.Content.StartsWith($"agree"))
                    {
                        try
                        {
                            await rawMessage.DeleteAsync();
                        }
                        catch (HttpException)
                        {
                        }
                    }
                    else
                    {
                        try
                        {
                            await Task.Delay(10000);
                            await rawMessage.DeleteAsync();
                        }
                        catch (HttpException)
                        {
                        }
                    }
                }
            }

            if (!rawMessage.Content.StartsWith("~report"))
            {
                await ProcessAttachments(db, rawMessage, channel);
            }

            await CheckTextDenylist(guild, rawMessage, guildConfig, templates);
            await CheckTextGreylist(guild, rawMessage, guildConfig, templates);
        }

        /// <summary>
        /// Check a message against the text denylist.
        /// </summary>
        private static async Task CheckTextDenylist(SocketGuild guild, IMessage rawMessage,
            DiscordGuildConfiguration guildConfig, ITemplateProvider templates)
        {
            foreach (var regexString in guildConfig.TextDenylist)
            {
                var match = Regex.Match(rawMessage.Content, regexString);
                if (match.Success)
                {
                    LastCaughtRegex = regexString;
                    await rawMessage.DeleteAsync();
                    await rawMessage.Author.SendMessageAsync(embed: templates.Execute("automod/delete.md", new
                        {
                            ChannelName = rawMessage.Channel.Name,
                            MessageText = rawMessage.Content,
                            Pattern = regexString,
                        })
                        .ToEmbedBuilder()
                        .WithColor(Color.Orange)
                        .Build());
                }
            }
        }

        /// <summary>
        /// Check a message against the text greylist.
        /// </summary>
        private static async Task CheckTextGreylist(SocketGuild guild, IMessage rawMessage,
            DiscordGuildConfiguration guildConfig, ITemplateProvider templates)
        {
            if (guildConfig.TextGreylist == null)
            {
                Log.Warning("{List} is null.", nameof(guildConfig.TextGreylist));
                return;
            }

            foreach (var regexString in guildConfig.TextGreylist)
            {
                var match = Regex.Match(rawMessage.Content, regexString);
                if (match.Success)
                {
                    LastCaughtRegex = regexString;
                    var reportChannel = guild.GetTextChannel(guildConfig.ReportChannel);
                    if (reportChannel == null)
                    {
                        Log.Warning("No report channel configured for softblocked message!");
                        return;
                    }

                    await reportChannel.SendMessageAsync(embed: templates.Execute("automod/softblock.md", new
                        {
                            ChannelName = rawMessage.Channel.Name,
                            MessageText = rawMessage.Content,
                            Pattern = regexString,
                            JumpLink = rawMessage.GetJumpUrl(),
                        })
                        .ToEmbedBuilder()
                        .WithColor(Color.Orange)
                        .Build());
                }
            }
        }

        /// <summary>
        /// Save attachments to a local directory. Remember to clear out this folder periodically.
        /// </summary>
        private static void SaveAttachments(IDbService db, WebClient wc, SocketMessage rawMessage)
        {
            if (!rawMessage.Attachments.Any()) return;

            if (!Directory.Exists(db.Config.TempDir))
            {
                Directory.CreateDirectory(db.Config.TempDir);
            }

            foreach (var a in rawMessage.Attachments)
            {
                wc.DownloadFile(new Uri(a.Url), Path.Combine(db.Config.TempDir, a.Filename));
                Log.Information("Saved attachment {Filename}", Path.Combine(db.Config.TempDir, a.Filename));
            }
        }

        /// <summary>
        /// Convert attachments that don't render automatically to formats that do.
        /// </summary>
        private static async Task ProcessAttachments(IDbService db, SocketMessage rawMessage,
            IGuildChannel guildChannel)
        {
            if (!rawMessage.Attachments.Any()) return;

            foreach (var attachment in rawMessage.Attachments)
            {
                var justFileName =
                    attachment.Filename[..attachment.Filename.LastIndexOf(".", StringComparison.InvariantCulture)];
                if (attachment.Filename.ToLower().EndsWith(".bmp") || attachment.Filename.ToLower().EndsWith(".dib"))
                {
                    try
                    {
                        var timer = new Stopwatch();
                        using var bitmap =
                            await SixLabors.ImageSharp.Image.LoadAsync(Path.Combine(db.Config.TempDir,
                                attachment.Filename));
                        await bitmap.SaveAsPngAsync(Path.Combine(db.Config.TempDir, justFileName + ".png"));
                        timer.Stop();
                        Log.Information("Processed BMP from {DiscordName}, ({Time}ms)!",
                            $"{rawMessage.Author.Username}#{rawMessage.Author.Discriminator}",
                            timer.ElapsedMilliseconds);
                        await (guildChannel as ITextChannel).SendFileAsync(
                            Path.Combine(db.Config.TempDir, justFileName + ".png"),
                            $"{rawMessage.Author.Mention}: Your file has been automatically converted from BMP/DIB to PNG (BMP files don't render automatically).");
                    }
                    catch (FileNotFoundException)
                    {
                        Log.Error("Could not find file {Filename}",
                            Path.Combine(db.Config.TempDir, attachment.Filename));
                    }
                }
            }
        }
    }
}