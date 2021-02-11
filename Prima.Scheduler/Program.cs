using Prima.Scheduler.Services;
using Serilog;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Prima.Scheduler.GoogleApis.Services;

namespace Prima.Scheduler
{
    class Program
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

            client.MessageUpdated += events.OnMessageEdit;
            client.ReactionAdded += events.OnReactionAdd;
            client.ReactionRemoved += events.OnReactionRemove;

            services.GetRequiredService<RunNotiferService>().Initialize();
            services.GetRequiredService<AnnounceMonitor>().Initialize();

            Log.Information("Prima Scheduler logged in!");
            
            await Task.Delay(-1);
        }

        private static ServiceProvider ConfigureServices(IServiceCollection sc)
        {
            sc.AddSingleton<EventService>();
            sc.AddSingleton<RunNotiferService>();
            sc.AddSingleton<SpreadsheetService>();
            sc.AddSingleton<AnnounceMonitor>();
            sc.AddSingleton<CalendarApi>();
            return sc.BuildServiceProvider();
        }
    }
}