using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Serilog;
using VaderSharp;

namespace Prima.Stable
{
    public static class SentimentAnalysis
    {
        /// <summary>
        /// Reads Discord messages for their overall sentiment, firing a notice to the staff if something seems negative.
        /// DISCLAIMER: This does not imply action by the staff, only that they will pay more attention.
        /// </summary>
        public static async Task Handler(SentimentIntensityAnalyzer sentiment, SocketMessage message)
        {
            if (message.Source != MessageSource.User) return;

            var sChannel = message.Channel;
            if (!(sChannel is SocketGuildChannel channel)) return;

            var guild = channel.Guild;

            var analysisResult = sentiment.PolarityScores(message.Content);
            if (analysisResult.Compound < 0.5)
            {
                Log.Information("Negative message in {GuildName} (Score: {CompoundScore}): {Message}",
                    guild.Name, analysisResult.Compound, message.Content);
            }
        }
    }
}