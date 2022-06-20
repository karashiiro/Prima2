using Discord;
using Discord.WebSocket;
using Prima.DiscordNet.Extensions;
using Prima.Services;
using Serilog;
using System.Threading.Tasks;
using Discord.Net;
using Prima.Resources;
using Color = Discord.Color;

namespace Prima.Stable.Handlers
{
    public static class WelcomeCard
    {
        public static async Task Handler(DiscordSocketClient client, ITemplateProvider templates, SocketGuildUser user)
        {
            if (user == null)
            {
                Log.Information("A user joined, but the data received was null!");
                return;
            }

            if (user.Guild.Id == SpecialGuilds.CrystalExploratoryMissions)
            {
                try
                {
                    await user.SendMessageAsync(embed: templates.Execute("cemjoin.md", new
                        {
                            GuildName = user.Guild.Name,
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
                catch (HttpException e) when (e.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
                {
                    // ignored
                }
            }
        }
    }
}