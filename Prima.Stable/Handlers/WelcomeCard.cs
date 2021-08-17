using System.Linq;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Prima.DiscordNet.Extensions;
using Prima.Resources;
using Prima.Services;
using System.Threading.Tasks;
using Color = Discord.Color;

namespace Prima.Stable.Handlers
{
    public static class WelcomeCard
    {
        public static async Task Handler(IDiscordClient client, ITemplateProvider templates, SocketMessage message)
        {
            // This is being done this way rather than with the MEMBER_ADD gateway event because
            // that event just isn't being received. All intents are enabled, and the bot is not
            // in more than 100 servers. In another server, it worked fine for some reason.
            if (message.Channel is not IGuildChannel channel || channel.Guild.Id != SpecialGuilds.CrystalExploratoryMissions) return;

            IUser user = null;
            if (await channel.Guild.GetChannelAsync(857729033562226748) != null) // Kupo Bot join channel, posts 100% of the time
            {
                if (channel.Id == 857729033562226748 && message.Author.Id == 107256979105267712)
                {
                    // Try to get the message embed footer text
                    var embed = message.Embeds.FirstOrDefault();
                    var footer = embed?.Footer?.Text;
                    if (string.IsNullOrEmpty(footer)) return;

                    // Don't message people when they leave the server
                    var leaving = embed.Description.Contains("left");
                    if (leaving) return;

                    // Footer is formatted as: "User ID: 12345678901234567890"
                    var userIdMatch = new Regex(@"\d+").Match(footer);
                    if (!userIdMatch.Success) return;

                    if (!ulong.TryParse(userIdMatch.Value, out var userId)) return;

                    user = await client.GetUserAsync(userId);
                }
            }
            else if (channel.Id == channel.Guild.SystemChannelId && message.Source == MessageSource.System) // Fallback to system channel, sometimes doesn't post
            {
                user = message.Author;
            }

            if (user == null) return;

            await user.SendMessageAsync(embed: templates.Execute("cemjoin.md", new
            {
                GuildName = channel.Guild.Name,
                BotMention = client.CurrentUser.Mention,
                ContentRolesChannelLink = "<#590757405927669769>",
                HowDoesThisWorkChannelLink = "<#582762593865695243>",
                RulesChannelLink = "<#550707138348187648>",
            })
                .ToEmbedBuilder()
                .WithColor(Color.Orange)
                .Build());
        }
    }
}