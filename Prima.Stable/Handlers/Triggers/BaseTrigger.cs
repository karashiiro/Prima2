using System;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Prima.Stable.Handlers.Triggers
{
    public abstract class BaseTrigger
    {
        private readonly ulong _guildId;

        public BaseTrigger()
        {
            _guildId = 0; // Sentinel value for no guild
        }

        protected BaseTrigger(ulong guildId)
        {
            _guildId = guildId;
        }

        public abstract bool Condition(SocketMessage message);

        public abstract Task Execute(DiscordSocketClient client, SocketMessage message);

        public ulong GetApplicableGuildId()
        {
            return _guildId;
        }

        public bool IsGuildApplicable(SocketGuild guild)
        {
            return _guildId == 0 || _guildId == guild?.Id;
        }

        /// <summary>
        /// Helper method for detecting if a phrase contains a certain word.
        /// </summary>
        protected static bool HasWord(string phrase, string word)
        {
            var lowerPhrase = phrase.ToLower();
            return lowerPhrase.StartsWith($"{word} ") ||
                   lowerPhrase.EndsWith($" {word}") ||
                   lowerPhrase.Contains($" {word} ") ||
                   lowerPhrase == word;
        }
    }
}