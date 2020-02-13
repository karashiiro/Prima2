using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Prima.Attributes;
using Prima.Contexts;
using Prima.Services;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Prima.Modules
{
    /// <summary>
    /// This module includes commands that assist with moderation.
    /// </summary>
    [Name("Moderation")]
    [ConfigurationPreset(Preset.Moderation)]
    public class ModerationModule : ModuleBase<SocketCommandContext>
    {
        public ConfigurationService Config { get; set; }
        public EventService Events { get; set; }
        public HttpClient Http { get; set; }

        // Submit a report.
        [Command("report")]
        public async Task ReportAsync()
        {
            if (Context.Guild != null)
            {
                WarnOfPublicReport().Start();
            }
            // We're not awaiting this because we want to delete their message ASAP if they're in a guild.
            Context.Channel.SendMessageAsync(Properties.Resources.ReportThankYou).Start();
            SocketGuild guild = Context.Guild ?? Context.Client.GetGuild(Config.GetULong("" + Context.User.MutualGuilds.First().Id));
            SocketTextChannel postChannel = guild.GetTextChannel(Config.GetULong("" + guild.Id, "Channels", "reports"));
            string output = $"<@&{Config.GetSection("" + guild.Id, "Roles", "Moderator")}> {Context.User.Username}#{Context.User.Discriminator} just sent a report: {Context.Message.Content}";
            if (output.Length > 2000) // This can only be the case once, no need for a loop.
            {
                await postChannel.SendMessageAsync(output.Substring(0, 2000));
                output = output.Substring(2000);
            }
            await postChannel.SendMessageAsync(output);
            foreach (Attachment attachment in Context.Message.Attachments)
            {
                Stream attachmentData = await Http.GetStreamAsync(new Uri(attachment.Url));
                attachmentData.Seek(0, SeekOrigin.Begin);
                await postChannel.SendFileAsync(attachmentData, attachment.Filename, string.Empty);
            }
            await Context.Message.DeleteAsync();
        }

        private async Task WarnOfPublicReport()
        {
            IUserMessage warning = await ReplyAsync(Context.User.Mention + ", " + Properties.Resources.ReportInGuildWarning);
            await Task.Delay(5000);
            await warning.DeleteAsync();
        }

        // Check who a user is.
        [Command("whois", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task WhoIsAsync(IUser member) // Who knows?
        {
            if (member == null)
            {
                await ReplyAsync(Properties.Resources.MentionNotProvidedError);
                return;
            }

            using (var db = new DiscordXIVUserContext())
            {
                DiscordXIVUser found;
                try
                {
                    found = db.Users
                        .Single(user => user.DiscordId == member.Id);
                }
                catch (InvalidOperationException)
                {
                    await ReplyAsync(Properties.Resources.UserNotInDatabaseError);
                    return;
                }

                Embed responseEmbed = new EmbedBuilder()
                    .WithTitle($"({found.World}) {found.Name}")
                    .WithUrl($"https://na.finalfantasyxiv.com/lodestone/character/{found.LodestoneId}/")
                    .WithColor(Color.Blue)
                    .WithThumbnailUrl(found.Avatar)
                    .Build();

                await ReplyAsync(embed: responseEmbed);
                Log.Information("Successfully responded to whoami.");
            }
        }

        // Check when a user joined Discord.
        [Command("when")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task WhenAsync(IUser user)
        {
            if (user == null)
            {
                await ReplyAsync($"That's not a valid member. Usage: `{Config.GetSection("Prefix")}when <mention>`");
                return;
            }
            long timestampFromSnowflake = ((long)user.Id / 4194304) + 1420070400000;
            DateTime then = new DateTime(timestampFromSnowflake);
            await ReplyAsync(then.ToString());
            Log.Information("Successfully responded to whoami.");
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

            using (var db = new TextBlacklistContext())
            {
                var entry = new GuildTextBlacklistEntry();
                entry.GuildId = Context.Guild.Id;
                entry.RegexString = regexString;
                db.RegexStrings.Add(entry);
                await db.SaveChangesAsync();
            }

            await ReplyAsync(Properties.Resources.GenericSuccess);
        }

        // Remove a regex from the blacklist.
        [Command("unblocktext")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task UnblockTextAsync([Remainder] string regexString)
        {
            using var db = new TextBlacklistContext();
            if (string.IsNullOrEmpty(regexString)) // Remove the last regex that was matched if none was specified.
            {
                var entry = db.RegexStrings.Single(rs => rs.RegexString == Events.LastCaughtRegex);
                db.Remove(entry);
            }
            else
            {
                try
                {
                    var entry = db.RegexStrings.Single(rs => rs.RegexString == regexString);
                    db.Remove(entry);
                }
                catch (InvalidOperationException)
                {
                    await ReplyAsync(Properties.Resources.RegexNotFoundError);
                    return;
                }
            }
            await db.SaveChangesAsync();
            await ReplyAsync(Properties.Resources.GenericSuccess);
        }
    }
}
