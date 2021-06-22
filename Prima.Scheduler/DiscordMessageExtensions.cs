using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace Prima.Scheduler
{
    public static class DiscordMessageExtensions
    {
        public static async Task CreateReactionsAsync(this DiscordMessage message, DiscordEmoji[] emotes)
        {
            foreach (var emote in emotes)
            {
                await message.CreateReactionAsync(emote);
            }
        }
    }
}