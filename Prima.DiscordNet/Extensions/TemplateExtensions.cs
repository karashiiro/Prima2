using Discord;

namespace Prima.DiscordNet.Extensions
{
    public static class TemplateExtensions
    {
        public static EmbedBuilder ToEmbedBuilder(this ResolvedTemplate template)
        {
            var builder = new EmbedBuilder();
            var text = template.Text;

            if (text.StartsWith("#"))
            {
                var lines = text.Split('\n', '\r');
                builder.Title = lines[0].TrimStart('#').Trim();
                text = text[lines[0].Length..].Trim();
            }

            builder.Description = text;

            return builder;
        }
    }
}