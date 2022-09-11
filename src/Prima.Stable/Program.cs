using Discord.WebSocket;
using FFXIVWeather.Lumina;
using Lumina;
using Microsoft.Extensions.DependencyInjection;
using Prima.DiscordNet;
using Prima.Game.FFXIV;
using Prima.Game.FFXIV.FFLogs;
using Prima.Game.FFXIV.XIVAPI;
using Prima.Services;
using Serilog;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Prima.DiscordNet.Handlers;
using Prima.DiscordNet.Services;

namespace Prima.Stable
{
    public static class Program
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
            var keepClean = services.GetRequiredService<KeepClean>();
            var ephemeralPinner = services.GetRequiredService<EphemeralPinManager>();
            var templates = services.GetRequiredService<ITemplateProvider>();

            keepClean.Initialize();
            roleRemover.Initialize();
            ephemeralPinner.Initialize();
            await ffLogs.Initialize();

            client.MessageReceived += message => ChatCleanup.Handler(db, web, templates, message);

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