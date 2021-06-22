using DSharpPlus.Entities;
using System.Collections.Generic;

namespace Prima.Scheduler
{
    public class DiscordEmbedField
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool Inline { get; set; }

        public DiscordEmbedField WithName(string name)
        {
            Name = name;
            return this;
        }

        public DiscordEmbedField WithValue(string value)
        {
            Value = value;
            return this;
        }

        public DiscordEmbedField WithIsInline(bool inline)
        {
            Inline = inline;
            return this;
        }
    }

    public static class DiscordEmbedBuilderExtensions
    {
        public static DiscordEmbedBuilder WithFields(this DiscordEmbedBuilder builder, IEnumerable<DiscordEmbedField> fields)
        {
            foreach (var field in fields)
            {
                builder.AddField(field.Name, field.Value, field.Inline);
            }

            return builder;
        }
    }
}