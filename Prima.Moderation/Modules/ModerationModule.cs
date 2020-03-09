﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Prima.Models;
using Prima.Moderation.Services;
using Prima.Services;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Prima.Moderation.Modules
{
    /// <summary>
    /// This module includes commands that assist with moderation.
    /// </summary>
    [Name("Moderation")]
    public class ModerationModule : ModuleBase<SocketCommandContext>
    {
        public DbService Db { get; set; }
        public EventService Events { get; set; }
        public HttpClient Http { get; set; }

        // Submit a report.
        [Command("report", RunMode = RunMode.Async)]
        public async Task ReportAsync(params string[] p)
        {
            if (Context.Guild != null)
            {
                _ = WarnOfPublicReport();
            }
            var responseMessage = await Context.Channel.SendMessageAsync(Properties.Resources.ReportThankYou);

            SocketGuild guild = Context.Guild;
            if (guild == null)
            {
                foreach (var otherGuild in Context.User.MutualGuilds)
                {
                    if (Db.Guilds.Any(g => g.Id == otherGuild.Id))
                    {
                        guild = otherGuild;
                        break;
                    }
                }
            }
            var guildConfig = Db.Guilds.Single(g => g.Id == guild.Id);

            SocketTextChannel postChannel = guild.GetTextChannel(guildConfig.ReportChannel);
            string output = $"<@&{guildConfig.Roles["Moderator"]}> {Context.User.Username}#{Context.User.Discriminator} just sent a report:{Context.Message.Content.Substring(7)}";
            if (output.Length > 2000) // This can only be the case once, no need for a loop.
            {
                await postChannel.SendMessageAsync(output.Substring(0, 2000));
                output = output.Substring(2000);
            }
            await postChannel.SendMessageAsync(output);
            foreach (Attachment attachment in Context.Message.Attachments)
            {
                await postChannel.SendFileAsync(Path.Combine(Db.Config.TempDir, attachment.Filename), string.Empty);
            }
            if (Context.Guild != null)
            {
                await Context.Message.DeleteAsync();
                await Task.Delay(10000);
                await responseMessage.DeleteAsync();
            }
        }

        private async Task WarnOfPublicReport()
        {
            IUserMessage warning = await ReplyAsync(Context.User.Mention + ", " + Properties.Resources.ReportInGuildWarning);
            await Task.Delay(5000);
            await warning.DeleteAsync();
        }

        // Check when a user joined Discord.
        [Command("when")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
        public async Task WhenAsync(IUser user)
        {
            await ReplyAsync(user.CreatedAt.UtcDateTime.ToString());
        }

        // Add a regex to the blacklist.
        [Command("blocktext")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task BlockTextAsync([Remainder] string regexString)
        {
            try
            {
                Regex.IsMatch("", regexString);
            }
            catch (ArgumentException)
            {
                await ReplyAsync(Properties.Resources.InvalidRegexError);
                return;
            }

            await Db.AddGuildTextBlacklistEntry(Context.Guild.Id, regexString);
            await ReplyAsync(Properties.Resources.GenericSuccess);
        }

        // Remove a regex from the blacklist.
        [Command("unblocktext")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task UnblockTextAsync([Remainder] string regexString)
        {
            DiscordGuildConfiguration guildConfig;
            try
            {
                guildConfig = Db.Guilds.Single(g => g.Id == Context.Guild.Id);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            if (string.IsNullOrEmpty(regexString)) // Remove the last regex that was matched if none was specified.
            {
                var entry = guildConfig.TextBlacklist.Single(rs => rs == Events.LastCaughtRegex);
                await Db.RemoveGuildTextBlacklistEntry(Context.Guild.Id, entry);
            }
            else
            {
                try
                {
                    var entry = guildConfig.TextBlacklist.Single(rs => rs == regexString);
                     await Db.RemoveGuildTextBlacklistEntry(Context.Guild.Id, entry);
                }
                catch (InvalidOperationException)
                {
                    await ReplyAsync(Properties.Resources.RegexNotFoundError);
                    return;
                }
            }
            await ReplyAsync(Properties.Resources.GenericSuccess);
        }
    }
}