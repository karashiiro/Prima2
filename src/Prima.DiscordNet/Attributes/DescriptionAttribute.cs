using System;

namespace Prima.DiscordNet.Attributes
{
    /// <summary>
    /// Sets the description for the command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DescriptionAttribute : Attribute
    {
        public string Description { get; set; }

        public DescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}
