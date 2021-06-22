using Discord;
using System.Linq;
using System.Threading.Tasks;

namespace Prima.DiscordNet
{
    public static class AnnounceUtil
    {
        public static async Task<ulong?> GetEventId(Cacheable<IUserMessage, ulong> cachedMessage)
        {
            var message = await cachedMessage.GetOrDownloadAsync();
            var embed = message.Embeds.FirstOrDefault(e => e.Type == EmbedType.Rich);
            
            if (embed?.Footer == null) return null;

            if (!ulong.TryParse(embed.Footer?.Text, out var eventId)) return null;

            return eventId;
        }
    }
}
