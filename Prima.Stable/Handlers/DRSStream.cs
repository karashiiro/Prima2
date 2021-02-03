using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;

namespace Prima.Stable.Handlers
{
    public static class DRSStream
    {
        private static readonly Regex TwitchRegex = new Regex(@"https:\/\/www\.twitch\.tv\/\D+");

        public static async Task Handler(IMessage message)
        {
            if (message.Channel.Id != 806570453958262794) return;

            if (TwitchRegex.IsMatch(message.Content))
            {
                const int SECOND = 1000;
                const int MINUTE = 60 * SECOND;
                const int HOUR = 60 * MINUTE;
                const int DAY = 24 * HOUR;
                await Task.Delay(1 * DAY);
            }

            await message.DeleteAsync();
        }
    }
}