using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Prima.Attributes;
using Prima.Contexts;
using Prima.Services;
using Serilog;
using System;
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
            Log.Information("Added {DiscordName} to {Member}.", $"{Context.User.Username}#{Context.User.Discriminator}", memberRole.Name);
        }

        // Check who this user is.
        [Command("whoami", RunMode = RunMode.Async)]
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

                Log.Information("Answered whoami from ({World}) {Name}.", found.World, found.Name);

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
            Log.Information("There are {DBEntryCount} users in the database.", db.Users.Count());
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
            Log.Information(Properties.Resources.ClockAddSuccess);
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
            Log.Information(Properties.Resources.ClockRemoveSuccess);
        }
    }
}
