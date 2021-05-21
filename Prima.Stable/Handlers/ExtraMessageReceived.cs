using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Prima.Resources;

namespace Prima.Stable.Handlers
{
    public static class ExtraMessageReceived
    {
        public static async Task Handler(DiscordSocketClient client, SocketMessage message)
        {
            var cem = client.GetGuild(SpecialGuilds.CrystalExploratoryMissions);
            var isCEMChannel = cem?.GetTextChannel(message.Channel.Id) != null;

            var tasks = new List<Task>();

            if (isCEMChannel && (HasWord(message.Content, "intercardinals") || HasWord(message.Content, "intercards") || message.Content.Contains("383805961216983061")))
            {
                var emotes = new[] {new Emoji("↖️"), new Emoji("↙️"), new Emoji("↘️"), new Emoji("↗️") };
                foreach (var emote in emotes)
                {
                    await message.AddReactionAsync(emote);
                }
            }

            if (message.Content == "(╯°□°）╯︵ ┻━┻")
            {
                tasks.Add(message.Channel.SendMessageAsync("┬─┬ ノ( ゜-゜ノ)"));
            }

            if (isCEMChannel && (HasWord(message.Content, "sch") || HasWord(message.Content, "scholar")))
            {
                var emote = await cem.GetEmoteAsync(573531927613800459); // SCH emote
                tasks.Add(message.AddReactionAsync(emote));
            }

            if (isCEMChannel && message.Content.Contains("383805961216983061"))
            {
                var sadge = await cem.GetEmoteAsync(845161446547521566);
                tasks.Add(message.AddReactionAsync(sadge));
                tasks.Add(message.AddReactionAsync(new Emoji("🦶")));
            }

            await Task.WhenAll(tasks);
        }

        private static bool HasWord(string phrase, string word)
        {
            var lowerPhrase = phrase.ToLower();
            return lowerPhrase.StartsWith($"{word} ") ||
                   lowerPhrase.EndsWith($" {word}") ||
                   lowerPhrase.Contains($" {word} ") ||
                   lowerPhrase == word;
        }
    }
}
