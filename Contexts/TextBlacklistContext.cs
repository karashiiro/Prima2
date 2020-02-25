using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Prima.Contexts
{
    public class TextBlacklistContext : DbContext
    {
        public DbSet<GuildTextBlacklistEntry> RegexStrings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseNpgsql(
                $"Host={Environment.GetEnvironmentVariable("PRIMA_DB_HOST")};" +
                $"Database=TextBlacklistStore;" +
                $"Username={Environment.GetEnvironmentVariable("PRIMA_DB_USER")};" +
                $"Password={Environment.GetEnvironmentVariable("PRIMA_DB_PASS")}",
                npgsqlOpts => npgsqlOpts.EnableRetryOnFailure());

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
        protected override void OnModelCreating(ModelBuilder model)
            => model.Entity<GuildTextBlacklistEntry>()
                    .HasComment("This table contains regex strings tagged by guild for messages to be deleted immediately.")
                    .UseXminAsConcurrencyToken();
    }

    public class GuildTextBlacklistEntry
    {
        [Key]
        public string RegexString { get; set; }

        public ulong GuildId { get; set; }
    }
}
