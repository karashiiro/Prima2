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

            var outputChannel = guild.GetTextChannel(guildConfig.SentimentAnalysisChannel);
            if (outputChannel == null)
                Log.Warning("Output channel for sentiment analysis not configured.");

            var analysisResult = sentiment.PolarityScores(message.Content);
            if (analysisResult.Compound < guildConfig.SentimentAnalysisThreshold)
            {
                Log.Information("Negative message in {GuildName} #{ChannelName} (Score: {CompoundScore}): {Message}",
                    guild.Name, channel.Name, analysisResult.Compound, message.Content);
                if (outputChannel != null)
                {
                    var messageContent = message.Content;
                    if (messageContent.Length > 1800)
                        messageContent = messageContent.Substring(0, 1800);
                    var embed = new EmbedBuilder()
                        .WithDescription($"Negative message in <#{channel.Id}>: {message.GetJumpUrl()}\n" +
                                         $"Score: `{analysisResult.Compound}`\n" +
                                         "```\n" +
                                         $"{messageContent}\n" +
                                         "```")
                        .WithColor(Color.LightOrange)
                        .Build();
                    await outputChannel.SendMessageAsync(embed: embed);
                }
            }
        }
    }
}