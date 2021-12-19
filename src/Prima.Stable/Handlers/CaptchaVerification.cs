using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Prima.DiscordNet.Extensions;
using Prima.Resources;
using Prima.Services;
using Serilog;
using Color = Discord.Color;

namespace Prima.Stable.Handlers
{
    public static class CaptchaVerification
    {
        public static async Task Handler(DiscordSocketClient client, ITemplateProvider templates, Captcha captcha, SocketGuildUser user)
        {
            if (user == null)
            {
                Log.Information("A user joined, but the data received was null!");
                return;
            }

            if (user.Guild.Id == SpecialGuilds.CrystalExploratoryMissions)
            {
                var verificationImage = await captcha.Generate(user.Id.ToString());

                await user.SendMessageAsync(embed: templates.Execute<object>("captcha.md")
                    .ToEmbedBuilder()
                    .WithColor(Color.Orange)
                    .Build());

                await user.SendFileAsync(verificationImage, "captcha.png");
            }
        }
    }
}