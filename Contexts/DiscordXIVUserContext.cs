using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Prima.Contexts
{
    public class DiscordXIVUserContext : DbContext
    {
        public DbSet<DiscordXIVUser> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseNpgsql(
                $"Host={Environment.GetEnvironmentVariable("PRIMA_DB_HOST")};" +
                $"Database=DiscordXIVUserStore;" +
                $"Username={Environment.GetEnvironmentVariable("PRIMA_DB_USER")};" +
                $"Password={Environment.GetEnvironmentVariable("PRIMA_DB_PASS")}",
                npgsqlOpts => npgsqlOpts.EnableRetryOnFailure());

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
        protected override void OnModelCreating(ModelBuilder model)
            => model.Entity<DiscordXIVUser>()
                    .HasComment("This table contains the associations between Discord users and FFXIV characters.")
                    .UseXminAsConcurrencyToken();
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
