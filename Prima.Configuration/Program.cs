using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Prima.Configuration
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

                Log.Information($"Prima Configuration logged in!");
                
                /*var uptime = services.GetRequiredService<UptimeMessageService>();
                uptime.Initialize("Prima Configuration", "Wrestling with the database.");
                uptime.StartAsync().Start();*/
                
                await Task.Delay(-1);
            }
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private static ServiceProvider ConfigureServices(IServiceCollection sc)
        {
            //sc.AddSingleton<UptimeMessageService>();
            return sc.BuildServiceProvider();
        }
    }
}