using System.Threading.Tasks;
using System.Net;
using Discord.WebSocket;
using FFXIVWeather;
using Microsoft.Extensions.DependencyInjection;
using Prima.Services;
using Prima.Stable.Handlers;
using Prima.Stable.Services;
using Serilog;

namespace Prima.Stable
{
    static class Program
    {
        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        private static async Task MainAsync(string[] args)
        {
            var sc = CommonInitialize.Main(args);

            // Initialize the ASP.NET service provider and freeze this Task indefinitely.
            await using var services = ConfigureServices(sc);
            await CommonInitialize.ConfigureServicesAsync(services);

            var client = services.GetRequiredService<DiscordSocketClient>();
            var db = services.GetRequiredService<DbService>();
            var moderationEvents = services.GetRequiredService<ModerationEventService>();
            var censusEvents = services.GetRequiredService<CensusEventService>();
            var mute = services.GetRequiredService<MuteService>();

            client.ReactionAdded += (message, channel, reaction) =>  ReactionReceived.HandlerAdd(db, message, channel, reaction);
            client.ReactionRemoved += (message, channel, reaction) => ReactionReceived.HandlerRemove(db, message, channel, reaction);

            client.MessageDeleted += moderationEvents.MessageDeleted;
            client.MessageReceived += moderationEvents.MessageRecieved;

            client.MessageReceived += message => MessageCache.Handler(db, message);
            client.MessageReceived += message => ExtraMessageReceived.Handler(client, message);
            client.MessageReceived += DRSStream.Handler;

            client.GuildMemberUpdated += censusEvents.GuildMemberUpdated;

            client.UserVoiceStateUpdated += mute.OnVoiceJoin;

            Log.Information("Prima.Stable logged in!");
                
            await Task.Delay(-1);
        }

        private static ServiceProvider ConfigureServices(IServiceCollection sc)
        {
            sc
                .AddSingleton<WebClient>()
                .AddSingleton<ModerationEventService>()
                .AddSingleton<CensusEventService>()
                .AddSingleton<PresenceService>()
                .AddSingleton<XIVAPIService>()
                .AddSingleton<FFXIVWeatherService>()
                .AddSingleton<MuteService>();
            return sc.BuildServiceProvider();
        }
    }
}