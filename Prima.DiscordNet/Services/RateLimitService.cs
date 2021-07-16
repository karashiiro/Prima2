using Discord.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Prima.DiscordNet.Services
{
    public class RateLimitService
    {
        private readonly IDictionary<string, long> _commandTimeouts; // Key: command name, value: use time

        public RateLimitService()
        {
            _commandTimeouts = new ConcurrentDictionary<string, long>();
        }

        /// <summary>
        /// Returns the number of seconds until the specified command may be used again.
        /// </summary>
        /// <param name="command">The command to check.</param>
        public long TimeUntilReady(CommandInfo command)
        {
            if (!_commandTimeouts.ContainsKey(command.Name))
                return 0;
            return _commandTimeouts[command.Name] - DateTimeOffset.Now.ToUnixTimeSeconds();
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
        /// <param name="seconds">How long to disable the command.</param>
        public void ResetTime(CommandInfo command, int seconds)
        {
            if (seconds == 0)
                throw new InvalidOperationException("Command does not have a rate limit.");

            _commandTimeouts.Remove(command.Name);
            _commandTimeouts.Add(command.Name, DateTimeOffset.Now.ToUnixTimeSeconds() + seconds);
        }
    }
}