using DSharpPlus.Entities;

namespace Prima.Scheduler
{
    public static class DiscordEmbedExtensions
    {
        public static DiscordEmbedBuilder ToEmbedBuilder(this DiscordEmbed embed) => new(embed);
    }
}