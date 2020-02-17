using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Prima.Contexts;
using Prima.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Prima
{
    class Program
    {
        static void Main(string[] args) => new Program().MainAsync(args).GetAwaiter().GetResult();  

        public async Task MainAsync(string[] args)
        {
            // We set an option here to determine what bot preset we should run.
            Preset preset = Preset.Undefined;
            try {
                preset = (Preset)Enum.Parse(typeof(Preset), args[0]);
                if (preset == Preset.Undefined)
                {
                    throw new ArgumentException(Properties.Resources.UndefinedPresetError);
                }
            }
            catch (IndexOutOfRangeException e)
            {
                Console.Error.WriteLine(Properties.Resources.UndefinedPresetError);
                Console.Error.WriteLine(e);
                Environment.Exit(1);
            }
            catch (ArgumentNullException e)
            {
                Console.Error.WriteLine("Please provide a valid configuration preset.");
                Console.Error.WriteLine(e);
                Environment.Exit(1);
            }
            catch (ArgumentException e)
            {
                Console.Error.WriteLine(args[0] + " is not a valid preset.");
                Console.Error.WriteLine(e);
                Environment.Exit(1);
            }

            var disConfig = new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                LargeThreshold = 250,
                MessageCacheSize = 100,
            };

            // Initialize the ASP.NET service provider and freeze this Task indefinitely.
            using (ServiceProvider services = ConfigureServices(disConfig, preset))
            {
                // Initialize the static logger from configuration.
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .Enrich.WithProperty("System", services.GetRequiredService<ConfigurationService>().CurrentPreset)
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {System}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.SQLite(Properties.Resources.SerilogFilename)
                    .CreateLogger();

                var client = services.GetRequiredService<DiscordSocketClient>();
                var events = services.GetRequiredService<EventService>();

                client.Log += LogAsync;
                services.GetRequiredService<CommandService>().Log += LogAsync;

                client.GuildMemberUpdated += events.GuildMemberUpdated;
                client.MessagesBulkDeleted += async (IReadOnlyCollection<Cacheable<IMessage, ulong>> cmessages, ISocketMessageChannel ichannel) =>
                {
                    foreach (var cmessage in cmessages)
                    {
                        await events.MessageDeleted(cmessage, ichannel);
                    }
                };
                client.MessageDeleted += events.MessageDeleted;
                client.MessageReceived += events.MessageRecieved;
                client.ReactionAdded += events.ReactionAdded;
                client.ReactionRemoved += events.ReactionRemoved;

                await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_TOKEN"));
                await client.StartAsync();
                Log.Information($"Logged in with configuration preset {preset}.");
                
                if (preset == Preset.Clerical)
                {
                    await services.GetRequiredService<ServerClockService>().InitializeAsync();
                    services.GetRequiredService<ServerClockService>().Start();
                }
                else if (preset == Preset.Extra)
                {
                    services.GetRequiredService<PresenceService>().Start();
                }
                
                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

                await Task.Delay(-1);
            }
        }

        private Task LogAsync(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    Log.Error(message.ToString());
                    break;
                case LogSeverity.Error:
                    Log.Error(message.ToString());
                    break;
                case LogSeverity.Warning:
                    Log.Warning(message.ToString());
                    break;
                case LogSeverity.Info:
                    Log.Information(message.ToString());
                    break;
                case LogSeverity.Verbose:
                    Log.Verbose(message.ToString());
                    break;
                case LogSeverity.Debug:
                    Log.Debug(message.ToString());
                    break;
            }
            return Task.CompletedTask;
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private static ServiceProvider ConfigureServices(DiscordSocketConfig disConfig, Preset preset)
        {
            IServiceCollection sc = new ServiceCollection()
                // DB Contexts
                .AddDbContext<DiscordXIVUserContext>()
                .AddDbContext<TextBlacklistContext>()
                // Group 1 - No dependencies
                .AddSingleton(new ConfigurationService(preset))
                .AddSingleton(new DiscordSocketClient(disConfig))
                .AddSingleton<HttpClient>()
                .AddSingleton<LotoIdService>()
                .AddSingleton(SystemClock.Instance)
                .AddSingleton<WebClient>()
                // Group 2
                .AddSingleton<CommandService>()
                .AddSingleton<DiagnosticService>()
                .AddSingleton<PresenceService>()
                .AddSingleton<ServerClockService>()
                .AddSingleton<XIVAPIService>()
                // Group 3
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<EventService>();
            return sc.BuildServiceProvider();
        }
    }
}
