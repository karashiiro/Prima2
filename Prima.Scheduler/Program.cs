using Prima.Scheduler.Services;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

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

            Log.Information($"Prima Scheduler logged in!");
                
            /*var uptime = services.GetRequiredService<UptimeMessageService>();
                uptime.Initialize("Prima Scheduler", "Friendzoned from Google.");
                uptime.StartAsync().Start();*/
            
            await Task.Delay(-1);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private static ServiceProvider ConfigureServices(IServiceCollection sc)
        {
            sc.AddSingleton<EventService>();
            sc.AddSingleton<RunNotiferService>();
            //sc.AddSingleton<UptimeMessageService>();
            return sc.BuildServiceProvider();
        }
    }
}