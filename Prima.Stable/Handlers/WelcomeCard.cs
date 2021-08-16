using System.Linq;
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
        public static async Task Handler(ITemplateProvider templates, SocketMessage message)
        {
            // This is being done this way rather than with the MEMBER_ADD gateway event because
            // that event just isn't being received. All intents are enabled, and the bot is not
            // in more than 100 servers. In another server, it worked fine for some reason.
            if (message.Channel is not IGuildChannel channel || channel.Guild.Id != SpecialGuilds.CrystalExploratoryMissions) return;
            if (channel.Id != channel.Guild.SystemChannelId) return;

            var user = message.MentionedUsers.FirstOrDefault();
            if (user == null) return;

            await user.SendMessageAsync(embed: templates.Execute("cemjoin.md", new
            {
                //
            })
                .ToEmbedBuilder()
                .WithColor(Color.DarkOrange)
                .Build());
        }
    }
}