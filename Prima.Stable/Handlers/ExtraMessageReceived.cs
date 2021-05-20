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

            var emoteStorage1 = client.GetGuild(SpecialGuilds.EmoteStorage1);

            if (message.Content == "(╯°□°）╯︵ ┻━┻")
            {
                await message.Channel.SendMessageAsync("┬─┬ ノ( ゜-゜ノ)");
                return;
            }

            if (isCEMChannel && (HasWord(message.Content, "sch") || HasWord(message.Content, "scholar")))
            {
                var emote = await cem.GetEmoteAsync(573531927613800459); // SCH emote
                await message.AddReactionAsync(emote);
            }

            if (isCEMChannel && (HasWord(message.Content, "intercardinals") || HasWord(message.Content, "intercards") || message.Content.Contains("383805961216983061")))
            {
                var emotes = new[] {new Emoji("↖️"), new Emoji("↙️"), new Emoji("↘️"), new Emoji("↗️") };
                foreach (var emote in emotes)
                {
                    await message.AddReactionAsync(emote);
                }
            }

            if (isCEMChannel && emoteStorage1 != null && message.Content.Contains("383805961216983061"))
            {
                var emote = await emoteStorage1.GetEmoteAsync(844635607354966036);
                await message.AddReactionAsync(emote);
                await message.AddReactionAsync(new Emoji("🦶"));
            }
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
