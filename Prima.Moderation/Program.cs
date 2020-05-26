using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Prima.Moderation.Services;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;

namespace Prima.Moderation
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
            var messageCache = services.GetRequiredService<MessageCacheService>();

            client.MessageDeleted += events.MessageDeleted;
            client.MessageReceived += events.MessageRecieved;
            client.MessageReceived += messageCache.CacheMessage;

            Log.Information($"Prima Moderation logged in!");
                
            /*var uptime = services.GetRequiredService<UptimeMessageService>();
                uptime.Initialize("Prima Moderation", "Hammertime.");
                uptime.StartAsync().Start();*/
                
            await Task.Delay(-1);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private static ServiceProvider ConfigureServices(IServiceCollection sc)
        {
            sc.AddSingleton<WebClient>()
              .AddSingleton<EventService>()
              .AddSingleton<MessageCacheService>();
            //sc.AddSingleton<UptimeMessageService>();
            return sc.BuildServiceProvider();
        }
    }
}