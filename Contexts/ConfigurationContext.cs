using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Prima.Contexts
{
    public class ConfigurationContext : DbContext
    {
        public DbSet<ClockConfiguration> ClockData { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite(Properties.Resources.UWPConnectionStringConfiguration);
    }

    public class ClockConfiguration
    {
        [Key]
        public ulong ChannelId { get; set; }

        public ulong GuildId { get; set; }
        public string TzId { get; set; }
    }
}
