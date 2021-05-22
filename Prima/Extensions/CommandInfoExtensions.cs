using System.Linq;
using Discord.Commands;
using Prima.Attributes;

namespace Prima.Extensions
{
    public static class CommandInfoExtensions
    {
        /// <summary>
        /// Returns true if the provided command has a timeout.
        /// </summary>
        /// <param name="command">The command to check.</param>
        public static bool HasTimeout(this CommandInfo command)
        {
            var rateLimitInfo = (RateLimitAttribute)command?.Attributes.FirstOrDefault(attr => attr is RateLimitAttribute);
            return rateLimitInfo != null;
        }
    }
}