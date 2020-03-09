using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Prima.Clerical
{
    class Program
    {
        static void Main(string[] args) => new Program().MainAsync(args).GetAwaiter().GetResult();  

        public async Task MainAsync(string[] args)
        {
            IServiceCollection sc = CommonInitialize.Main(args);

            // Initialize the ASP.NET service provider and freeze this Task indefinitely.
            using (ServiceProvider services = ConfigureServices(sc))
            {
                await CommonInitialize.ConfigureServicesAsync(services);

                var client = services.GetRequiredService<DiscordSocketClient>();
                var events = services.GetRequiredService<EventService>();

                client.GuildMemberUpdated += events.GuildMemberUpdated;

                Log.Information($"Prima Census logged in!");
                
                /*var uptime = services.GetRequiredService<UptimeMessageService>();
                uptime.Initialize("Prima Clerical", "A lonelier cubicle.");
                uptime.StartAsync().Start();*/
                
                await Task.Delay(-1);
            }
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private static ServiceProvider ConfigureServices(IServiceCollection sc)
        {
            sc.AddSingleton<EventService>();
            //sc.AddSingleton<UptimeMessageService>();
            return sc.BuildServiceProvider();
        }
    }
}