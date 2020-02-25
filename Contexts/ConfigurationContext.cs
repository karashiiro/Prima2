using Microsoft.EntityFrameworkCore;
using NodaTime;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Prima.Contexts
{
    public class ConfigurationContext : DbContext
    {
        public DbSet<ClockConfiguration> ClockData { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseNpgsql(
                $"Host={Environment.GetEnvironmentVariable("PRIMA_DB_HOST")};" +
                $"Database=ConfigurationStore;" +
                $"Username={Environment.GetEnvironmentVariable("PRIMA_DB_USER")};" +
                $"Password={Environment.GetEnvironmentVariable("PRIMA_DB_PASS")}",
                npgsqlOpts =>
                {
                    npgsqlOpts.EnableRetryOnFailure();
                    npgsqlOpts.UseNodaTime();
                });

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
        protected override void OnModelCreating(ModelBuilder model)
            => model.Entity<ClockConfiguration>()
                    .HasComment("This table contains the Discord server clock objects.")
                    .UseXminAsConcurrencyToken();
    }

    public class ClockConfiguration
    {
        [Key]
        public ulong ChannelId { get; set; }

        public ulong GuildId { get; set; }
        public ZonedClock Timezone { get; set; }
    }
}
