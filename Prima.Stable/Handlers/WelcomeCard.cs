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
            // If the Kupo Bot join log channel exists, use it and ignore the system log.
            // Kupo Bot always posts join and leave notifications, as opposed to the system log
            // which only sometimes posts join messages. Kupo Bot doesn't have the aforementioned
            // MEMBER_ADD event issue.
            const ulong kupoBotJoinLog = 857729033562226748;
            if (await channel.Guild.GetChannelAsync(kupoBotJoinLog) != null)
            {
                const ulong kupoBot = 107256979105267712;
                if (channel.Id == kupoBotJoinLog && message.Author.Id == kupoBot)
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
            else if (channel.Id == channel.Guild.SystemChannelId && message.Source == MessageSource.System)
            {
                user = message.Author;
            }

            if (user == null) return;

            await user.SendMessageAsync(embed: templates.Execute("cemjoin.md", new
            {
                GuildName = channel.Guild.Name,
                BotMention = client.CurrentUser.Mention,
                ContentRolesChannelLink = "<#590757405927669769>",
                HowDoesThisWorkChannelLink = "<#877659281481162803>",
                RulesChannelLink = "<#550707138348187648>",
                HelpChannelLink = "<#550777867173232661>",
                OtherUsefulServersChannelLink = "<#569322805351415808>",
            })
                .ToEmbedBuilder()
                .WithColor(Color.Orange)
                .Build());
        }
    }
}