using Microsoft.Extensions.DependencyInjection;
using Prima.Game.FFXIV.FFLogs.Services;

namespace Prima.Game.FFXIV.FFLogs.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds log parsing services to the dependency injection container
        /// </summary>
        public static IServiceCollection AddLogParsingServices(this IServiceCollection services)
        {
            services.AddTransient<ILogParsingService, LogParsingService>();
            return services;
        }
    }
}
