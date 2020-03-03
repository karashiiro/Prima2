using Microsoft.Extensions.DependencyInjection;
using Prima.Scheduler.Services;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Prima.Scheduler
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

                Log.Information($"Prima Scheduler logged in!");
                
                /*var uptime = services.GetRequiredService<UptimeMessageService>();
                uptime.Initialize("Prima Scheduler", "Friendzoned from Google.");
                uptime.StartAsync().Start();*/
                
                await Task.Delay(-1);
            }
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private static ServiceProvider ConfigureServices(IServiceCollection sc)
        {
            sc.AddSingleton<SchedulingService>();
            //sc.AddSingleton<UptimeMessageService>();
            return sc.BuildServiceProvider();
        }
    }
}