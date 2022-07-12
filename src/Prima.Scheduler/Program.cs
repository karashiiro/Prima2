using Microsoft.Extensions.DependencyInjection;
using Prima.DiscordNet;
using Serilog;
using System.Threading.Tasks;
using Prima.GoogleApis.Services;

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