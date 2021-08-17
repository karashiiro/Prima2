using Discord.WebSocket;
using FFXIVWeather.Lumina;
using Lumina;
using Microsoft.Extensions.DependencyInjection;
using Prima.DiscordNet;
using Prima.Game.FFXIV;
using Prima.Game.FFXIV.FFLogs;
using Prima.Game.FFXIV.XIVAPI;
using Prima.Services;
using Prima.Stable.Handlers;
using Prima.Stable.Services;
using Serilog;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Prima.Stable
{
    static class Program
    {
        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        private static async Task MainAsync(string[] args)
        {
            var sc = CommonInitialize.Main();

            // Initialize the ASP.NET service provider and freeze this Task indefinitely.
            await using var services = ConfigureServices(sc);
            await CommonInitialize.ConfigureServicesAsync(services);

            var client = services.GetRequiredService<DiscordSocketClient>();
            var db = services.GetRequiredService<IDbService>();
            var censusEvents = services.GetRequiredService<CensusEventService>();
            var mute = services.GetRequiredService<MuteService>();
            var roleRemover = services.GetRequiredService<TimedRoleManager>();
            var ffLogs = services.GetRequiredService<FFLogsClient>();
            var web = services.GetRequiredService<WebClient>();
            var lodestone = services.GetRequiredService<CharacterLookup>();
            var keepClean = services.GetRequiredService<KeepClean>();
            var ephemeralPinner = services.GetRequiredService<EphemeralPinManager>();
            var templates = services.GetRequiredService<ITemplateProvider>();

            keepClean.Initialize();
            roleRemover.Initialize();
            ephemeralPinner.Initialize();
            await ffLogs.Initialize();

            client.ReactionAdded += (message, channel, reaction)
                => ReactionReceived.HandlerAdd(db, lodestone, message, channel, reaction);
            client.ReactionRemoved += (message, channel, reaction)
                => ReactionReceived.HandlerRemove(db, message, channel, reaction);

            client.ReactionAdded += (message, _, reaction)
                => VoteReactions.HandlerAdd(client, db, message, reaction);

            client.MessageDeleted += (message, channel) => AuditDeletion.Handler(db, client, message, channel);
            client.MessageReceived += message => ChatCleanup.Handler(db, web, templates, message);
            client.MessageReceived += message => WelcomeCard.Handler(client, templates, message);

            client.MessageReceived += message => MessageCache.Handler(db, message);
            client.MessageReceived += message => TriggerDispatcher.Handler(client, message);

            //client.UserJoined += user => WelcomeCard.Handler(templates, user);

            client.GuildMemberUpdated += censusEvents.GuildMemberUpdated;

            client.UserVoiceStateUpdated += mute.OnVoiceJoin;

            Log.Information("Prima.Stable logged in!");

            await Task.Delay(-1);
        }

        private static ServiceProvider ConfigureServices(IServiceCollection sc)
        {
            sc
                .AddSingleton<WebClient>()
                .AddSingleton<CensusEventService>()
                .AddSingleton<PresenceService>()
                .AddSingleton<XIVAPIClient>()
                .AddSingleton(new GameData(Environment.OSVersion.Platform == PlatformID.Win32NT
                    ? @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack"
                    : Path.Combine(Environment.GetEnvironmentVariable("HOME"), "sqpack"),
                    new LuminaOptions
                    {
                        PanicOnSheetChecksumMismatch = false,
                    }))
                .AddSingleton<FFXIVWeatherLuminaService>()
                .AddSingleton<MuteService>()
                .AddSingleton<TimedRoleManager>()
                .AddSingleton<FFLogsClient>()
                .AddSingleton<CharacterLookup>()
                .AddSingleton<KeepClean>()
                .AddSingleton<EphemeralPinManager>();
            return sc.BuildServiceProvider();
        }
    }
}