using Microsoft.Extensions.DependencyInjection;
using Prima.Extra.Services;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Prima.Extra
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

                Log.Information($"Prima Extra logged in!");
                
                /*var uptime = services.GetRequiredService<UptimeMessageService>();
                uptime.Initialize("Prima Extra", "Omake freestyle!");
                uptime.StartAsync().Start();*/

                services.GetRequiredService<PresenceService>().Start();
                
                await Task.Delay(-1);
            }
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private static ServiceProvider ConfigureServices(IServiceCollection sc)
        {
            sc.AddSingleton<PresenceService>();
              //.AddSingleton<UptimeMessageService>();
            return sc.BuildServiceProvider();
        }
    }
}