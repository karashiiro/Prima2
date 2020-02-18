using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Prima.Contexts
{
    public class TextBlacklistContext : DbContext
    {
        public DbSet<GuildTextBlacklistEntry> RegexStrings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite(Properties.Resources.UWPConnectionStringTextBlacklist);
    }

    public class GuildTextBlacklistEntry
    {
        [Key]
        public string RegexString { get; set; }

        public ulong GuildId { get; set; }
    }
}
