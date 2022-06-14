﻿using System.Net;
using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FFXIVWeather.Lumina;
using Lumina;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prima.Application.Scheduling;
using Prima.DiscordNet.Services;
using Prima.Game.FFXIV;
using Prima.Game.FFXIV.FFLogs;
using Prima.Game.FFXIV.XIVAPI;
using Prima.Queue.Services;
using Prima.Scheduler.GoogleApis.Services;
using Prima.Scheduler.Handlers;
using Prima.Services;
using Prima.Stable.Handlers;
using Prima.Stable.Services;
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
        sc.AddSingleton<DiagnosticService>();
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
        });

        sc.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });
    })
    .UseConsoleLifetime()
    .Build();

// Fit our logger onto Discord.NET's logging interface
var logger = host.Services.GetRequiredService<ILogger<DiscordSocketClient>>();

Task LogAsync(LogMessage message)
{
    switch (message.Severity)
    {
        case LogSeverity.Critical:
            logger.LogError(message.Exception, $"{message.Source}: {message.Message}");
            break;
        case LogSeverity.Error:
            logger.LogError(message.Exception, $"{message.Source}: {message.Message}");
            break;
        case LogSeverity.Warning:
            logger.LogWarning(message.Exception, $"{message.Source}: {message.Message}");
            break;
        case LogSeverity.Info:
            logger.LogInformation(message.Exception, $"{message.Source}: {message.Message}");
            break;
        case LogSeverity.Verbose:
            logger.LogTrace(message.Exception, $"{message.Source}: {message.Message}");
            break;
        case LogSeverity.Debug:
            logger.LogDebug(message.Exception, $"{message.Source}: {message.Message}");
            break;
        default:
            throw new InvalidOperationException($"Invalid log level \"{message.Severity}\".");
    }
    
    return Task.CompletedTask;
}

var client = host.Services.GetRequiredService<DiscordSocketClient>();

client.Log += LogAsync;
host.Services.GetRequiredService<CommandService>().Log += LogAsync;

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
await commandHandler.InitializeAsync(Assembly.GetAssembly(typeof(Prima.Queue.Program)));
await commandHandler.InitializeAsync(Assembly.GetAssembly(typeof(Prima.Scheduler.Program)));
await commandHandler.InitializeAsync(Assembly.GetAssembly(typeof(Prima.Stable.Program)));
await commandHandler.InitializeAsync();

// TODO: Refactor all of the callbacks below into more cohesive modules

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

// Add the old Prima.Scheduler callbacks
var calendar = host.Services.GetRequiredService<CalendarApi>();

client.MessageUpdated += (_, message, _) => AnnounceEdit.Handler(client, calendar, db, message);
client.ReactionAdded += (cachedMessage, _, reaction)
    => AnnounceReact.HandlerAdd(client, db, cachedMessage, reaction);

// Skip the old Prima.Queue services and callbacks since we aren't using them right now

// Run the host
await host.RunAsync();