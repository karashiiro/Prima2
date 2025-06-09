using System.Net;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using FFXIVWeather.Lumina;
using Google.Apis.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NetStone;
using NetStone.GameData.Lumina;
using Prima.Application;
using Prima.Application.Community;
using Prima.Application.Community.CrystalExploratoryMissions;
using Prima.Application.Moderation;
using Prima.Application.Personality;
using Prima.Application.Scheduling;
using Prima.Application.Scheduling.Calendar;
using Prima.Application.Scheduling.Events;
using Prima.Data;
using Prima.DiscordNet.Handlers;
using Prima.DiscordNet.Logging;
using Prima.DiscordNet.Services;
using Prima.Game.FFXIV.FFLogs;
using Prima.Game.FFXIV.FFLogs.Rules;
using Prima.Game.FFXIV.XIVAPI;
using Prima.Services;
using Quartz;

var googleApiSecretPath = Environment.GetEnvironmentVariable("PRIMA_GOOGLE_SECRET") ??
                          throw new ArgumentException("No PRIMA_GOOGLE_SECRET variable set!");
var googleApiTokenDirectory = Environment.GetEnvironmentVariable("PRIMA_SESSION_STORE") ??
                              throw new ArgumentException("No PRIMA_SESSION_STORE variable set!");
var (googleApiCredential, googleApiCredentialException) =
    await GoogleApiAuth.AuthorizeSafely(googleApiSecretPath, googleApiTokenDirectory);
var (calendarConfig, calendarConfigException) = await CalendarConfig.FromFileSafely(
    Environment.GetEnvironmentVariable("PRIMA_CALENDAR_CONFIG") ??
    throw new ArgumentException("No PRIMA_CALENDAR_CONFIG variable set!"));

var gameDataPath = LuminaLoader.GetGameDataPath();
var gameData = LuminaLoader.Load(gameDataPath);
var gameDataDir = new DirectoryInfo(gameDataPath);
var gameDataProvider = new LuminaGameDataProvider(gameDataDir);
var lodestone = await LodestoneClient.GetClientAsync(gameDataProvider);

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((_, sc) =>
    {
        var disConfig = new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            LargeThreshold = 250,
            MessageCacheSize = 10000,
            GatewayIntents = GatewayIntents.All ^ GatewayIntents.GuildPresences ^ GatewayIntents.GuildScheduledEvents ^
                             GatewayIntents.GuildInvites,
        };

        sc.AddSingleton(_ => new DiscordSocketClient(disConfig));
        sc.AddSingleton<CommandService>();
        sc.AddSingleton<CommandHandlingService>();
        sc.AddSingleton<InteractionService>();
        sc.AddSingleton<InteractionHandlingService>();

        sc.AddSingleton<HttpClient>();
        sc.AddSingleton<IMongoClient, MongoClient>(_ => new MongoClient("mongodb://localhost:27017"));
        sc.AddSingleton<IRoleReactionsDb, RoleReactionsDb>();
        sc.AddSingleton<IDbService, DbService>();
        sc.AddSingleton<RateLimitService>();
        sc.AddSingleton<ITemplateProvider, TemplateProvider>();

        sc.AddSingleton(calendarConfig ?? new CalendarConfig());
        sc.AddSingleton((IConfigurableHttpClientInitializer?)googleApiCredential ?? new DummyCredential());
        sc.AddSingleton<GoogleCalendarClient>();

        sc.AddSingleton<WebClient>();
        sc.AddSingleton<CensusEventService>();
        sc.AddSingleton<XIVAPIClient>();
        sc.AddSingleton(gameData);
        sc.AddSingleton(lodestone);

        sc.AddSingleton<FFXIVWeatherLuminaService>();
        sc.AddSingleton<MuteService>();
        sc.AddSingleton<TimedRoleManager>();
        sc.AddSingleton<IFFLogsClient, FFLogsClient>();
        sc.AddSingleton<ILogParsingRulesSelector, DefaultLogParsingRulesSelector>();
        sc.AddSingleton<ILogParserService, LogParserService>();
        sc.AddSingleton<KeepClean>();
        sc.AddSingleton<EphemeralPinManager>();

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
Task LogDiscord<TService>(LogMessage message)
{
    var logger = host.Services.GetRequiredService<ILogger<TService>>();
    DiscordLogAdapter.Handler(logger, message.Severity, message.Exception, message.Source, message.Message);
    return Task.CompletedTask;
}

var client = host.Services.GetRequiredService<DiscordSocketClient>();
client.Log += LogDiscord<DiscordSocketClient>;

host.Services.GetRequiredService<CommandService>().Log += LogDiscord<CommandService>;

var db = host.Services.GetRequiredService<IDbService>();
var web = host.Services.GetRequiredService<WebClient>();
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

client.MessageUpdated += (_, message, _) => AnnounceEdit.Handler(client, db, message);
client.ReactionAdded += (cachedMessage, _, reaction)
    => AnnounceReact.HandlerAdd(client, db, cachedMessage, reaction);

// Set up actions that need to run after the bot starts
client.Ready += () =>
    TaskUtils.Detach(async () =>
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        if (googleApiCredentialException != null)
        {
            logger.LogError(googleApiCredentialException, "Failed to exchange Google API credentials");
        }

        if (calendarConfigException != null)
        {
            logger.LogError(calendarConfigException, "Failed to load calendar configuration");
        }

        logger.LogInformation("Client ready; initializing additional services");

        // Add interactions
        var interactionService = host.Services.GetRequiredService<InteractionService>();
        interactionService.Log += LogDiscord<InteractionService>;

        var interactionHandler = host.Services.GetRequiredService<InteractionHandlingService>();
        await interactionHandler.InitializeAsync();

        logger.LogInformation("Interaction service initialized with {SlashCommandCount} slash command(s)",
            interactionService.SlashCommands.Count);

        // Add text commands
        var commandHandler = host.Services.GetRequiredService<CommandHandlingService>();
        await commandHandler.InitializeAsync();

        // Set up other services
        var keepClean = host.Services.GetRequiredService<KeepClean>();
        var roleRemover = host.Services.GetRequiredService<TimedRoleManager>();
        var ephemeralPinner = host.Services.GetRequiredService<EphemeralPinManager>();
        var ffLogs = host.Services.GetRequiredService<IFFLogsClient>();

        keepClean.Initialize();
        roleRemover.Initialize();
        ephemeralPinner.Initialize();
        await ffLogs.Initialize();

        logger.LogInformation("Initialization complete - all systems ready!");
    });

// Ensure that we have a bot token
var token = Environment.GetEnvironmentVariable("PRIMA_BOT_TOKEN");
if (string.IsNullOrEmpty(token))
{
    throw new ArgumentException("No bot token provided!");
}

// Start the bot
await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("PRIMA_BOT_TOKEN"));
await client.StartAsync();

// Run the host
await host.RunAsync();