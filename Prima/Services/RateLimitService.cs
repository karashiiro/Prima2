using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Discord.Commands;
using Prima.Attributes;
using Prima.Extensions;

namespace Prima.Services
{
    public class RateLimitService
    {
        private readonly IDictionary<string, long> _commandTimeouts; // Key: command name, value: use time

        public RateLimitService()
        {
            _commandTimeouts = new ConcurrentDictionary<string, long>();
        }

        /// <summary>
        /// Returns true if the command is ready to be used again.
        /// </summary>
        /// <param name="command">The command to check.</param>
        public bool IsReady(CommandInfo command)
        {
            return !_commandTimeouts.ContainsKey(command.Name)
                   || _commandTimeouts[command.Name] < DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Makes the provided command globally unusable until its rate limit timer expires.
        /// Throws if the command has no rate limit.
        /// TODO: Make this per-guild or per-channel.
        /// </summary>
        /// <param name="command">The command to render unusable.</param>
        public void ResetTime(CommandInfo command)
        {
            if (!command.HasTimeout())
                throw new InvalidOperationException("Command does not have RateLimitAttribute set.");

            var rateLimitInfo = (RateLimitAttribute)command.Attributes.First(attr => attr is RateLimitAttribute);

            _commandTimeouts.Remove(command.Name);
            _commandTimeouts.Add(command.Name, DateTimeOffset.Now.ToUnixTimeSeconds() + rateLimitInfo.TimeSeconds);
        }
    }
}