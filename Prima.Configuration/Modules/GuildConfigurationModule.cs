using Discord;
using Discord.Commands;
using Prima.Services;
using System.Threading.Tasks;

namespace Prima.Configuration.Modules
{
    /// <summary>
    /// Includes guild configuration commands that only guild administrators should be able to execute.
    /// </summary>
    [Name("GuildConfiguration")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class GuildConfigurationModule : ModuleBase<SocketCommandContext>
    {
        public DbService Db { get; set; }

        public async Task ConfigureAsync(string key, string value)
        {
            await Db.SetGuildConfigurationProperty(Context.Guild.Id, key, value);
        }
    }
}
