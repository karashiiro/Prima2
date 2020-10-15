using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Prima.Queue.Services;

namespace Prima.Queue
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

            Log.Information($"Prima Queue logged in!");

            await Task.Delay(-1);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private static ServiceProvider ConfigureServices(IServiceCollection sc)
        {
            return sc
                .AddSingleton<FFXIV3RoleQueueService>()
                .BuildServiceProvider();
        }
    }
}