using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Prima.Services;
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
        public static async Task Handler(DbService db, SentimentIntensityAnalyzer sentiment, SocketMessage message)
        {
            if (message.Source != MessageSource.User) return;

            var sChannel = message.Channel;
            if (!(sChannel is SocketGuildChannel channel)) return;

            var guild = channel.Guild;
            var guildConfig = db.Guilds.FirstOrDefault(g => g.Id == guild.Id);
            if (guildConfig == null) return;

            var analysisResult = sentiment.PolarityScores(message.Content);
            if (analysisResult.Compound < guildConfig.SentimentAnalysisThreshold)
            {
                Log.Information("Negative message in {GuildName} (Score: {CompoundScore}): {Message}",
                    guild.Name, analysisResult.Compound, message.Content);
            }
        }
    }
}