using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Prima.DiscordNet;
using Prima.Scheduler.GoogleApis.Services;
using Prima.Scheduler.Handlers;
using Prima.Scheduler.Services;
using Prima.Services;
using Serilog;
using System.Threading.Tasks;

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
            var db = services.GetRequiredService<IDbService>();
            var calendar = services.GetRequiredService<CalendarApi>();

            client.MessageUpdated += events.OnMessageEdit;
            client.ReactionAdded += events.OnReactionAdd;
            client.ReactionRemoved += events.OnReactionRemove;

            client.MessageUpdated += (_, message, channel) => AnnounceEdit.Handler(client, calendar, db, message);
            client.ReactionAdded += (cachedMessage, channel, reaction)
                => AnnounceReact.HandlerAdd(client, db, cachedMessage, reaction);

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