using System.Threading.Tasks;
using Discord.WebSocket;
using Prima.Resources;

namespace Prima.Stable
{
    public static class ExtraMessageReceived
    {
        public static async Task Handler(DiscordSocketClient client, SocketMessage message)
        {
            if (message.Content == "(╯°□°）╯︵ ┻━┻")
            {
                await message.Channel.SendMessageAsync("┬─┬ ノ( ゜-゜ノ)");
                return;
            }

            if (HasWord(message.Content, "sch"))
            {
                var guild = client.GetGuild(SpecialGuilds.CrystalExploratoryMissions);
                var emote = await guild.GetEmoteAsync(573531927613800459); // SCH emote
                await message.AddReactionAsync(emote);
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
