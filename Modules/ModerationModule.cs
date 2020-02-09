using Discord;
using Discord.Commands;
using Prima.Attributes;
using Prima.Contexts;
using Prima.Services;
using System;
using System.Linq;
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

        // Check who a user is.
        [Command("whois")]
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
        }
    }
}
