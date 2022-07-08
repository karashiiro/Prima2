using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Prima.DiscordNet;
using Prima.Scheduler.GoogleApis.Services;
using Prima.Scheduler.Handlers;
using Prima.Services;
using Serilog;
using System.Threading.Tasks;

namespace Prima.Scheduler
{
    public static class Program
    {
        public static void Main() => MainAsync().GetAwaiter().GetResult();

        private static async Task MainAsync()
        {
            var sc = CommonInitialize.Main();

            // Initialize the ASP.NET service provider and freeze this Task indefinitely.
            await using var services = ConfigureServices(sc);
            await CommonInitialize.ConfigureServicesAsync(services);

            var client = services.GetRequiredService<DiscordSocketClient>();
            var db = services.GetRequiredService<IDbService>();
            var calendar = services.GetRequiredService<CalendarApi>();

            client.MessageUpdated += (_, message, _) => AnnounceEdit.Handler(client, calendar, db, message);
            
            Log.Information("Prima Scheduler logged in!");

            await Task.Delay(-1);
        }

        private static ServiceProvider ConfigureServices(IServiceCollection sc)
        {
            sc.AddSingleton<CalendarApi>();
            return sc.BuildServiceProvider();
        }
    }
}