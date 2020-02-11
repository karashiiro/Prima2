using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Prima.Services;
using Serilog;
using System;
using System.Collections.Generic;
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

            // Initialize the logger.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.SQLite(Properties.Resources.UWPConnectionString)
                .CreateLogger();

            // Initialize the ASP.NET service provider and freeze this Task indefinitely.
            using (ServiceProvider services = ConfigureServices(preset))
            {
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
                Console.WriteLine($"Logged in with configuration preset {preset}.");

                await client.DownloadUsersAsync(client.Guilds);
                await services.GetRequiredService<EventService>().InitializeAsync();

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

        private static ServiceProvider ConfigureServices(Preset preset)
        {
            IServiceCollection sc = new ServiceCollection()
                // Group 1 - No dependencies
                .AddSingleton(new ConfigurationService(preset))
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<HttpClient>()
                .AddSingleton<LotoIdService>()
                .AddSingleton<SystemClock>()
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
