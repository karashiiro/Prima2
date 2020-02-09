using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Prima.Contexts
{
    public class TextBlacklistContext : DbContext
    {
        public DbSet<GuildTextBlacklistEntry> RegexStrings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite(Properties.Resources.UWPConnectionString);
    }

    public class GuildTextBlacklistEntry
    {
        [Key]
        public ulong GuildId { get; set; }

        public string RegexString { get; set; }
    }
}
