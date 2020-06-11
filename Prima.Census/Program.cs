using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Prima.Census.Services;
using Prima.Services;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Prima.Census
{
    class Program
    {
        static void Main(string[] args) => new Program().MainAsync(args).GetAwaiter().GetResult();  

        public async Task MainAsync(string[] args)
        {
            var sc = CommonInitialize.Main(args);

            // Initialize the ASP.NET service provider and freeze this Task indefinitely.
            using (var services = ConfigureServices(sc))
            {
                await CommonInitialize.ConfigureServicesAsync(services);

                var client = services.GetRequiredService<DiscordSocketClient>();
                var events = services.GetRequiredService<EventService>();

                client.GuildMemberUpdated += events.GuildMemberUpdated;

                Log.Information($"Prima Census logged in!");
                
                /*var uptime = services.GetRequiredService<UptimeMessageService>();
                uptime.Initialize("Prima Census", "A lonely cubicle.");
                uptime.StartAsync().Start();*/
                
                await Task.Delay(-1);
            }
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private static ServiceProvider ConfigureServices(IServiceCollection sc)
        {
            sc.AddSingleton<EventService>()
              .AddSingleton<XIVAPIService>();
            //sc.AddSingleton<UptimeMessageService>();
            return sc.BuildServiceProvider();
        }
    }
}