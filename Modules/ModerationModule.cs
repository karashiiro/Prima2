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

        public TextBlacklistContext Blacklist { get; set; }

        // Submit a report.
        [Command("report", RunMode = RunMode.Async)]
        public async Task ReportAsync(params string[] p)
        {
            if (Context.Guild != null)
            {
                _ = WarnOfPublicReport();
            }
            var responseMessage = await Context.Channel.SendMessageAsync(Properties.Resources.ReportThankYou);
            SocketGuild guild = Context.Guild ?? Context.Client.GetGuild(Config.GetULong("" + Context.User.MutualGuilds.First().Id));
            SocketTextChannel postChannel = guild.GetTextChannel(Config.GetULong("" + guild.Id, "Channels", "reports"));
            string output = $"<@&{Config.GetSection("" + guild.Id, "Roles", "Moderator").Value}> {Context.User.Username}#{Context.User.Discriminator} just sent a report:{Context.Message.Content.Substring(7)}";
            if (output.Length > 2000) // This can only be the case once, no need for a loop.
            {
                await postChannel.SendMessageAsync(output.Substring(0, 2000));
                output = output.Substring(2000);
            }
            await postChannel.SendMessageAsync(output);
            foreach (Attachment attachment in Context.Message.Attachments)
            {
                await postChannel.SendFileAsync(Path.Combine(Config.TempDir, attachment.Filename), string.Empty);
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

            var entry = new GuildTextBlacklistEntry
            {
                GuildId = Context.Guild.Id,
                RegexString = regexString
            };
            Blacklist.RegexStrings.Add(entry);
            await Blacklist.SaveChangesAsync();

            await ReplyAsync(Properties.Resources.GenericSuccess);
        }

        // Remove a regex from the blacklist.
        [Command("unblocktext")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task UnblockTextAsync([Remainder] string regexString)
        {
            if (string.IsNullOrEmpty(regexString)) // Remove the last regex that was matched if none was specified.
            {
                var entry = Blacklist.RegexStrings.Single(rs => rs.RegexString == Events.LastCaughtRegex);
                Blacklist.Remove(entry);
            }
            else
            {
                try
                {
                    var entry = Blacklist.RegexStrings.Single(rs => rs.RegexString == regexString);
                    Blacklist.Remove(entry);
                }
                catch (InvalidOperationException)
                {
                    await ReplyAsync(Properties.Resources.RegexNotFoundError);
                    return;
                }
            }
            await Blacklist.SaveChangesAsync();
            await ReplyAsync(Properties.Resources.GenericSuccess);
        }
    }
}
