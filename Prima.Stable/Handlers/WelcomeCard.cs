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