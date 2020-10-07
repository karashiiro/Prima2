using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Prima.Stable.Services;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Prima.Services;
using System.Net;
using FFXIVWeather;

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
            var clericalEvents = services.GetRequiredService<ClericalEventService>();
            var moderationEvents = services.GetRequiredService<ModerationEventService>();
            var censusEvents = services.GetRequiredService<CensusEventService>();
            var mute = services.GetRequiredService<MuteService>();

            client.ReactionAdded += clericalEvents.ReactionAdded;
            client.ReactionRemoved += clericalEvents.ReactionRemoved;

            client.MessageDeleted += moderationEvents.MessageDeleted;
            client.MessageReceived += moderationEvents.MessageRecieved;

            client.GuildMemberUpdated += censusEvents.GuildMemberUpdated;

            client.UserVoiceStateUpdated += mute.OnVoiceJoin;

            Log.Information("Prima.Stable logged in!");
                
            /*var uptime = services.GetRequiredService<UptimeMessageService>();
                uptime.Initialize("Prima Clerical", "A lonelier cubicle.");
                uptime.StartAsync().Start();*/
                
            await Task.Delay(-1);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private static ServiceProvider ConfigureServices(IServiceCollection sc)
        {
            sc.AddSingleton<WebClient>()
              .AddSingleton<ModerationEventService>()
              .AddSingleton<MessageCacheService>()
              .AddSingleton<CensusEventService>()
              .AddSingleton<ClericalEventService>()
              .AddSingleton<PresenceService>()
              .AddSingleton<XIVAPIService>()
              .AddSingleton<FFXIVWeatherService>()
              .AddSingleton<MuteService>();
            //sc.AddSingleton<UptimeMessageService>();
            return sc.BuildServiceProvider();
        }
    }
}