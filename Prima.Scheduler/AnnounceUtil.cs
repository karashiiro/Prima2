using System.Linq;
using DSharpPlus.Entities;

namespace Prima.Scheduler
{
    public static class AnnounceUtil
    {
        public static ulong? GetEventId(DiscordMessage message)
        {
            var embed = message.Embeds.FirstOrDefault();
            
            if (embed?.Footer == null) return null;

            if (!ulong.TryParse(embed.Footer?.Text, out var eventId)) return null;

            return eventId;
        }
    }
}
