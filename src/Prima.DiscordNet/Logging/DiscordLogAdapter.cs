using System;
using Discord;
using Microsoft.Extensions.Logging;

namespace Prima.DiscordNet.Logging
{
    public static class DiscordLogAdapter
    {
        public static void Handler<TService>(ILogger<TService> logger, LogSeverity severity, Exception exception, string source, string message)
        {
            if (logger is null)
            {
                throw new InvalidOperationException($"{nameof(logger)} is null.");
            }

            Action<Exception, string, object[]> logFunc = severity switch
            {
                LogSeverity.Critical => logger.LogError,
                LogSeverity.Error => logger.LogError,
                LogSeverity.Warning => logger.LogWarning,
                LogSeverity.Info => logger.LogInformation,
                LogSeverity.Verbose => logger.LogTrace,
                LogSeverity.Debug => logger.LogDebug,
                _ => throw new ArgumentOutOfRangeException(nameof(severity)),
            };

            logFunc(exception, "{Source}: {Message}", new object[] { source, message });
        }
    }
}