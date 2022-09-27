using System.Net;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FFXIVWeather.Lumina;
using Lumina;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prima.Application;
using Prima.Application.Community;
using Prima.Application.Community.CrystalExploratoryMissions;
using Prima.Application.Moderation;
using Prima.Application.Personality;
using Prima.Application.Scheduling;
using Prima.Application.Scheduling.Events;
using Prima.DiscordNet.Handlers;
using Prima.DiscordNet.Services;
using Prima.Game.FFXIV;
using Prima.Game.FFXIV.FFLogs;
using Prima.Game.FFXIV.XIVAPI;
using Prima.GoogleApis.Services;
using Prima.Services;
using Quartz;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((_, sc) =>
    {
        var disConfig = new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            LargeThreshold = 250,
            MessageCacheSize = 10000,
            GatewayIntents = GatewayIntents.All,
        };

        sc.AddSingleton(_ => new DiscordSocketClient(disConfig));
        sc.AddSingleton<CommandService>();
        sc.AddSingleton<CommandHandlingService>();
        sc.AddSingleton<HttpClient>();
        sc.AddSingleton<IDbService, DbService>();
        sc.AddSingleton<RateLimitService>();
        sc.AddSingleton<ITemplateProvider, TemplateProvider>();

        // Add old Prima.Stable services
        sc.AddSingleton<WebClient>();
        sc.AddSingleton<CensusEventService>();
        sc.AddSingleton<XIVAPIClient>();
        if (Environment.GetEnvironmentVariable("FFXIV_HOME") == null)
        {
            sc.AddSingleton(_ => new GameData(Environment.OSVersion.Platform == PlatformID.Win32NT
                    ? @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack"
                    : Path.Combine(Environment.GetEnvironmentVariable("HOME")
                                   ?? throw new ArgumentException("No HOME variable set!"), "sqpack"),
                new LuminaOptions { PanicOnSheetChecksumMismatch = false }));
        }
        else
        {
            var home = Environment.GetEnvironmentVariable("FFXIV_HOME") ??
                       throw new ArgumentException("No FFXIV_HOME variable set!");
            sc.AddSingleton(_ => new GameData(home, new LuminaOptions { PanicOnSheetChecksumMismatch = false }));
        }

        sc.AddSingleton<FFXIVWeatherLuminaService>();
        sc.AddSingleton<MuteService>();
        sc.AddSingleton<TimedRoleManager>();
        sc.AddSingleton<FFLogsClient>();
        sc.AddSingleton<CharacterLookup>();
        sc.AddSingleton<KeepClean>();
        sc.AddSingleton<EphemeralPinManager>();

        // Add old Prima.Scheduler services
        sc.AddSingleton<CalendarApi>();

        // Add old Prima.Queue services
        sc.AddSingleton<PasswordGenerator>();

        // Add scheduler
        sc.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();

            q.ScheduleJob<CheckDelubrumSavageEventsJob>(
                t => t
                    .WithIdentity("drsEventsTrigger")
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()),
                j => j
                    .WithIdentity("drsEventsJob")
                    .WithDescription("Scheduled DRS Events Check"));

            q.ScheduleJob<CheckDelubrumNormalEventsJob>(
                t => t
                    .WithIdentity("drnEventsTrigger")
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()),
                j => j
                    .WithIdentity("drnEventsJob")
                    .WithDescription("Scheduled DRN Events Check"));

            q.ScheduleJob<CheckBozjaEventsJob>(
                t => t
                    .WithIdentity("bozZadEventsTrigger")
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()),
                j => j
                    .WithIdentity("bozZadEventsJob")
                    .WithDescription("Scheduled Bozja/Zadnor Events Check"));

            q.ScheduleJob<CheckCastrumEventsJob>(
                t => t
                    .WithIdentity("castrumEventsTrigger")
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()),
                j => j
                    .WithIdentity("castrumEventsJob")
                    .WithDescription("Scheduled Castrum Events Check"));

            q.ScheduleJob<CheckSocialEventsJob>(
                t => t
                    .WithIdentity("socialEventsTrigger")
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()),
                j => j
                    .WithIdentity("socialEventsJob")
                    .WithDescription("Scheduled Social Events Check"));

            q.ScheduleJob<UpdatePresenceJob>(
                t => t
                    .WithIdentity("updatePresenceTrigger")
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInMinutes(30).RepeatForever()),
                j => j
                    .WithIdentity("updatePresenceJob")
                    .WithDescription("Update Presence"));
        });

        sc.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });
    })
    .UseConsoleLifetime()
    .Build();

// Fit our logger onto Discord.NET's logging interface
var logger = host.Services.GetRequiredService<ILogger<DiscordSocketClient>>();

void LogDiscordMessage(LogSeverity severity, Exception exception, string source, string message)
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

Task LogDiscord(LogMessage message)
{
    LogDiscordMessage(message.Severity, message.Exception, message.Source, message.Message);
    return Task.CompletedTask;
}

var client = host.Services.GetRequiredService<DiscordSocketClient>();

client.Log += LogDiscord;
host.Services.GetRequiredService<CommandService>().Log += LogDiscord;

// Ensure that we have a bot token
var token = Environment.GetEnvironmentVariable("PRIMA_BOT_TOKEN");
if (string.IsNullOrEmpty(token))
{
    throw new ArgumentException("No bot token provided!");
}

// Start the bot
await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("PRIMA_BOT_TOKEN"));
await client.StartAsync();

var commandHandler = host.Services.GetRequiredService<CommandHandlingService>();
await commandHandler.InitializeAsync();

// Initialize the old Prima.Stable services
var keepClean = host.Services.GetRequiredService<KeepClean>();
var roleRemover = host.Services.GetRequiredService<TimedRoleManager>();
var ephemeralPinner = host.Services.GetRequiredService<EphemeralPinManager>();
var ffLogs = host.Services.GetRequiredService<FFLogsClient>();

keepClean.Initialize();
roleRemover.Initialize();
ephemeralPinner.Initialize();
await ffLogs.Initialize();

// Add the old Prima.Stable callbacks
var db = host.Services.GetRequiredService<IDbService>();
var web = host.Services.GetRequiredService<WebClient>();
var lodestone = host.Services.GetRequiredService<CharacterLookup>();
var templates = host.Services.GetRequiredService<ITemplateProvider>();
var censusEvents = host.Services.GetRequiredService<CensusEventService>();
var mute = host.Services.GetRequiredService<MuteService>();

client.ReactionAdded += (message, channel, reaction) =>
    TaskUtils.Detach(() => ReactionReceived.HandlerAdd(client, db, lodestone, message, channel, reaction));
client.ReactionRemoved += (message, channel, reaction) =>
    TaskUtils.Detach(() => ReactionReceived.HandlerRemove(db, message, channel, reaction));

client.ReactionAdded += (message, _, reaction)
    => VoteReactions.HandlerAdd(client, db, message, reaction);

client.MessageDeleted += (message, channel) =>
    TaskUtils.Detach(() => AuditDeletion.Handler(db, client, message, channel));

client.MessageReceived += message => ChatCleanup.Handler(db, web, templates, message);

client.MessageReceived += message => TaskUtils.Detach(() => MessageCache.Handler(db, message));
client.MessageReceived += message => TriggerDispatcher.Handler(client, message);

client.UserJoined += user => WelcomeCard.Handler(client, templates, user);

client.GuildMemberUpdated += censusEvents.GuildMemberUpdated;
client.GuildMemberUpdated += AddRelatedContentRole.Handler;

client.UserVoiceStateUpdated += mute.OnVoiceJoin;

client.ButtonExecuted += component => Modmail.Handler(db, component);

// Add the old Prima.Scheduler callbacks
var calendar = host.Services.GetRequiredService<CalendarApi>();

client.MessageUpdated += (_, message, _) => AnnounceEdit.Handler(client, calendar, db, message);
client.ReactionAdded += (cachedMessage, _, reaction)
    => AnnounceReact.HandlerAdd(client, db, cachedMessage, reaction);

// Run the host
await host.RunAsync();