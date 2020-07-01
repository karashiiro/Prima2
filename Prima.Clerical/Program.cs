using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Prima.Clerical.Services;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Prima.Services;

namespace Prima.Clerical
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
            var events = services.GetRequiredService<EventService>();

            var censusEvents = services.GetRequiredService<CensusEventService>();

            client.ReactionAdded += events.ReactionAdded;
            client.ReactionRemoved += events.ReactionRemoved;

            client.GuildMemberUpdated += censusEvents.GuildMemberUpdated;

            Log.Information("Prima Clerical logged in!");
                
            /*var uptime = services.GetRequiredService<UptimeMessageService>();
                uptime.Initialize("Prima Clerical", "A lonelier cubicle.");
                uptime.StartAsync().Start();*/
                
            await Task.Delay(-1);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private static ServiceProvider ConfigureServices(IServiceCollection sc)
        {
            sc.AddSingleton<CensusEventService>()
              .AddSingleton<EventService>()
              .AddSingleton<PresenceService>()
              .AddSingleton<XIVAPIService>();
            //sc.AddSingleton<UptimeMessageService>();
            return sc.BuildServiceProvider();
        }
    }
}