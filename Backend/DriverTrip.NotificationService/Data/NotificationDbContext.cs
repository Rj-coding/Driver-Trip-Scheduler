using DriverTrip.NotificationService.Models;
using Microsoft.EntityFrameworkCore;

namespace DriverTrip.NotificationService.Data
{
    /// <summary>
    /// The NotificationService owns only its idempotency slice of the shared database.
    /// It uses a SEPARATE EF migrations history table so its migrations never collide
    /// with the API's AppDbContext migrations that live in the same DriverTripDBKafka.
    /// </summary>
    public sealed class NotificationDbContext : DbContext
    {
        /// <summary>Dedicated history table so this context's migrations are isolated.</summary>
        public const string MigrationsHistoryTable = "__EFMigrationsHistory_Notification";

        public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProcessedEvent>(entity =>
            {
                entity.ToTable("ProcessedEvents");
                entity.HasKey(e => new { e.ConsumerName, e.EventId });
                entity.Property(e => e.ConsumerName).HasMaxLength(200);
                entity.Property(e => e.EventType).HasMaxLength(200);
            });
        }
    }
}
