using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Prima.Attributes;
using Prima.Contexts;
using Prima.Services;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Prima.Modules
{
    /// <summary>
    /// This includes generic server operation functions such as numberkeeping, registering (XIVAPI calls are in a different module), and reporting.
    /// </summary>
    [Name("Clerical")]
    [ConfigurationPreset(Preset.Clerical)]
    public class ClericalModule : ModuleBase<SocketCommandContext>
    {
        public ConfigurationService Config { get; set; }
        public HttpClient Http { get; set; }
        public ServerClockService Clocks { get; set; }

        // If they've registered, this adds them to the Member group.
        [Command("agree")]
        [RequireUserInDatabase]
        public async Task AgreeAsync()
        {
            if (Config.GetULong(Context.Guild.Id.ToString(), "Channels", "welcome") != Context.Channel.Id) return;
            SocketGuildUser user = Context.Guild.GetUser(Context.User.Id);
            SocketRole memberRole = Context.Guild.GetRole(Config.GetULong(Context.Guild.Id.ToString(), "Roles", "Member"));
            await user.AddRoleAsync(memberRole);
            Console.WriteLine($"Added {Context.User.Username}#{Context.User.Discriminator} to {memberRole.Name}.");
        }

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

        // Check who this user is.
        [Command("whoami")]
        public async Task WhoAmIAsync()
        {
            using (var db = new DiscordXIVUserContext())
            {
                DiscordXIVUser found;
                try
                {
                    found = db.Users
                        .Single(user => user.DiscordId == Context.User.Id);
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
            }
        }

        // Check the number of database entries.
        [Command("indexcount")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task IndexCountAsync()
        {
            await ReplyAsync(Properties.Resources.DBUserCountInProgress);
            using var db = new DiscordXIVUserContext();
            await ReplyAsync($"There are {db.Users.Count()} users in the database.");
        }

        // Add a clock to a voice channel.
        [Command("addclock")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddClockAsync(ulong channelId, string tzId)
        {
            if (Context.Guild == null)
            {
                await ReplyAsync(Properties.Resources.MustBeUsedInGuildError);
                return;
            }
            if (Context.Guild.GetChannel(channelId) is SocketTextChannel)
            {
                await ReplyAsync(Properties.Resources.MustBeUsedOnVoiceChannelError);
                return;
            }
            try
            {
                await Clocks.AddClock(Context.Guild.Id, channelId, tzId);
            }
            catch (ArgumentNullException)
            {
                await ReplyAsync(Properties.Resources.NotATimezoneIdError);
                return;
            }
            await ReplyAsync(Properties.Resources.ClockAddSuccess);
        }

        // Remove a clock from a voice channel.
        [Command("removeclock")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RemoveClockAsync(ulong channelId)
        {
            if (Context.Guild == null)
            {
                await ReplyAsync(Properties.Resources.MustBeUsedInGuildError);
                return;
            }
            if (Context.Guild.GetChannel(channelId) is SocketTextChannel)
            {
                await ReplyAsync(Properties.Resources.MustBeUsedOnVoiceChannelError);
                return;
            }
            await Clocks.RemoveClock(channelId);
            await ReplyAsync(Properties.Resources.ClockRemoveSuccess);
        }
    }
}
