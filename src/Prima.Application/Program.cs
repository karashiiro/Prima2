using System.Net;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FFXIVWeather.Lumina;
using Lumina;
using Microsoft.Extensions.DependencyInjection;
using Prima.Application.Logging;
using Prima.DiscordNet.Services;
using Prima.Game.FFXIV;
using Prima.Game.FFXIV.FFLogs;
using Prima.Game.FFXIV.XIVAPI;
using Prima.Queue.Services;
using Prima.Scheduler.GoogleApis.Services;
using Prima.Scheduler.Handlers;
using Prima.Scheduler.Services;
using Prima.Services;
using Prima.Stable.Handlers;
using Prima.Stable.Services;
using Quartz;

var disConfig = new DiscordSocketConfig
{
    AlwaysDownloadUsers = true,
    LargeThreshold = 250,
    MessageCacheSize = 10000,
    GatewayIntents = GatewayIntents.All,
};

// Set up services for dependency injection
var sc = new ServiceCollection();
sc.AddSingleton(_ => new DiscordSocketClient(disConfig));
sc.AddSingleton<CommandService>();
sc.AddSingleton<CommandHandlingService>();
sc.AddSingleton<DiagnosticService>();
sc.AddSingleton<IAppLogger, AppLogger>();
sc.AddSingleton<HttpClient>();
sc.AddSingleton<IDbService, DbService>();
sc.AddSingleton<RateLimitService>();
sc.AddSingleton<ITemplateProvider, TemplateProvider>();

// TODO: Refactor all of the services below into more cohesive modules

// Add old Prima.Stable services
sc.AddSingleton<WebClient>();
sc.AddSingleton<CensusEventService>();
sc.AddSingleton<PresenceService>();
sc.AddSingleton<XIVAPIClient>();
sc.AddSingleton(_ => new GameData(Environment.OSVersion.Platform == PlatformID.Win32NT
        ? @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack"
        : Path.Combine(Environment.GetEnvironmentVariable("HOME")
                       ?? throw new ArgumentException("No HOME variable set!"), "sqpack"), 
    new LuminaOptions { PanicOnSheetChecksumMismatch = false }));
sc.AddSingleton<FFXIVWeatherLuminaService>();
sc.AddSingleton<MuteService>();
sc.AddSingleton<TimedRoleManager>();
sc.AddSingleton<FFLogsClient>();
sc.AddSingleton<CharacterLookup>();
sc.AddSingleton<KeepClean>();
sc.AddSingleton<EphemeralPinManager>();

// Add old Prima.Scheduler services
sc.AddSingleton<AnnounceMonitor>();
sc.AddSingleton<CalendarApi>();

// Add old Prima.Queue services
sc.AddSingleton<FFXIV3RoleQueueService>();
sc.AddSingleton<QueueAnnouncementMonitor>();
sc.AddSingleton<ExpireQueuesService>();
sc.AddSingleton<PasswordGenerator>();

// Add scheduler
sc.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();
});

var services = sc.BuildServiceProvider();

// Fit our logger onto Discord.NET's logging interface
var logger = services.GetRequiredService<IAppLogger>();

Task LogAsync(LogMessage message)
{
    switch (message.Severity)
    {
        case LogSeverity.Critical:
            logger.Fatal(message.Exception, $"{message.Source}: {message.Message}");
            break;
        case LogSeverity.Error:
            logger.Error(message.Exception, $"{message.Source}: {message.Message}");
            break;
        case LogSeverity.Warning:
            logger.Warn(message.Exception, $"{message.Source}: {message.Message}");
            break;
        case LogSeverity.Info:
            logger.Info(message.Exception, $"{message.Source}: {message.Message}");
            break;
        case LogSeverity.Verbose:
            logger.Verbose(message.Exception, $"{message.Source}: {message.Message}");
            break;
        case LogSeverity.Debug:
            logger.Debug(message.Exception, $"{message.Source}: {message.Message}");
            break;
        default:
            throw new InvalidOperationException($"Invalid log level \"{message.Severity}\".");
    }
    
    return Task.CompletedTask;
}

var client = services.GetRequiredService<DiscordSocketClient>();

client.Log += LogAsync;
services.GetRequiredService<CommandService>().Log += LogAsync;

// Ensure that we have a bot token
var token = Environment.GetEnvironmentVariable("PRIMA_BOT_TOKEN");
if (string.IsNullOrEmpty(token))
{
    throw new ArgumentException("No bot token provided!");
}

// Start the bot
await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("PRIMA_BOT_TOKEN"));
await client.StartAsync();

logger.Info("Prima is now logged in!");

await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

// TODO: Refactor all of the callbacks below into more cohesive modules

// Initialize the old Prima.Stable services
var keepClean = services.GetRequiredService<KeepClean>();
var roleRemover = services.GetRequiredService<TimedRoleManager>();
var ephemeralPinner = services.GetRequiredService<EphemeralPinManager>();
var ffLogs = services.GetRequiredService<FFLogsClient>();

keepClean.Initialize();
roleRemover.Initialize();
ephemeralPinner.Initialize();
await ffLogs.Initialize();

// Add the old Prima.Stable callbacks
var db = services.GetRequiredService<IDbService>();
var web = services.GetRequiredService<WebClient>();
var lodestone = services.GetRequiredService<CharacterLookup>();
var templates = services.GetRequiredService<ITemplateProvider>();
var censusEvents = services.GetRequiredService<CensusEventService>();
var mute = services.GetRequiredService<MuteService>();

client.ReactionAdded += (message, channel, reaction)
    => ReactionReceived.HandlerAdd(client, db, lodestone, message, channel, reaction);
client.ReactionRemoved += (message, channel, reaction)
    => ReactionReceived.HandlerRemove(db, message, channel, reaction);

client.ReactionAdded += (message, _, reaction)
    => VoteReactions.HandlerAdd(client, db, message, reaction);

client.MessageDeleted += (message, channel) => AuditDeletion.Handler(db, client, message, channel);
client.MessageReceived += message => ChatCleanup.Handler(db, web, templates, message);

client.MessageReceived += message => MessageCache.Handler(db, message);
client.MessageReceived += message => TriggerDispatcher.Handler(client, message);

client.UserJoined += user => WelcomeCard.Handler(client, templates, user);

client.GuildMemberUpdated += censusEvents.GuildMemberUpdated;
client.GuildMemberUpdated += AddRelatedContentRole.Handler;

client.UserVoiceStateUpdated += mute.OnVoiceJoin;

client.ButtonExecuted += component => Modmail.Handler(db, component);

// Initialize the old Prima.Scheduler services
services.GetRequiredService<AnnounceMonitor>().Initialize();

// Add the old Prima.Scheduler callbacks
var calendar = services.GetRequiredService<CalendarApi>();

client.MessageUpdated += (_, message, _) => AnnounceEdit.Handler(client, calendar, db, message);
client.ReactionAdded += (cachedMessage, _, reaction)
    => AnnounceReact.HandlerAdd(client, db, cachedMessage, reaction);

// Skip the old Prima.Queue services and callbacks since we aren't using them right now

// Suspend the entrypoint task forever
await Task.Delay(-1);