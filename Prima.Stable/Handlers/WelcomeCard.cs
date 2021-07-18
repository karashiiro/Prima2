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
        public static async Task Handler(ITemplateProvider templates, SocketGuildUser user)
        {
            if (user.Guild.Id != SpecialGuilds.CrystalExploratoryMissions) return;

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