using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Prima.Contexts
{
    public class DiscordXIVUserContext : DbContext
    {
        public DbSet<DiscordXIVUser> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite(Properties.Resources.UWPConnectionStringDXIVUsers);
    }

    public class DiscordXIVUser
    {
        [Key]
        public ulong DiscordId { get; set; }

        public ulong LodestoneId { get; set; }
        public string World { get; set; }
        public string Name { get; set; }
        public string Avatar { get; set; }
    }
}
