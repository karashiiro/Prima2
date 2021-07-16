using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Prima.DiscordNet.Services;
using Prima.Services;
using Serilog.Events;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Prima.DiscordNet
{
    public static class CommonInitialize
    {
        public static IServiceCollection Main(string[] args)
        {
            StaticLog.Initialize();

            var disConfig = new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                LargeThreshold = 250,
                MessageCacheSize = 10000,
            };

            return ConfigurePartialServiceCollection(disConfig);
        }

        public static async Task ConfigureServicesAsync(ServiceProvider services)
        {
            static Task LogAsync(LogMessage message)
            {
                StaticLog.Write(ConvertEventLevel(message.Severity), message.Exception, message.Message);
                return Task.CompletedTask;
            }

            var client = services.GetRequiredService<DiscordSocketClient>();

            client.Log += LogAsync;
            services.GetRequiredService<CommandService>().Log += LogAsync;

            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("PRIMA_BOT_TOKEN"));
            await client.StartAsync();

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
        }

        private static IServiceCollection ConfigurePartialServiceCollection(DiscordSocketConfig disConfig)
        {
            return new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(disConfig))
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<DiagnosticService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<IDbService, DbService>()
                .AddSingleton<FFXIVSheetService>()
                .AddSingleton<PasswordGenerator>()
                .AddSingleton<RateLimitService>()
                .AddSingleton<ITemplateProvider, TemplateProvider>();
            //.AddSingleton(new HttpServer(Log.Information))
        }

        private static LogEventLevel ConvertEventLevel(LogSeverity level)
        {
            return level switch
            {
                LogSeverity.Critical => LogEventLevel.Fatal,
                LogSeverity.Error => LogEventLevel.Error,
                LogSeverity.Warning => LogEventLevel.Warning,
                LogSeverity.Info => LogEventLevel.Information,
                LogSeverity.Verbose => LogEventLevel.Verbose,
                LogSeverity.Debug => LogEventLevel.Debug,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
            };
        }
    }
}
