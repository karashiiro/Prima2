using Discord.Commands;
using Prima.Services;

namespace Prima.Configuration.Modules
{
    /// <summary>
    /// Includes global configuration commands that only the bot administrator should be able to execute.
    /// </summary>
    [Name("GlobalConfiguration")]
    [RequireOwner]
    public class GlobalConfigurationModule : ModuleBase<SocketCommandContext>
    {
        public DbService Db { get; set; }
    }
}
