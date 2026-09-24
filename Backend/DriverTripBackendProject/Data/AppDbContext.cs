using DriverTripBackendProject.Models;
using DriverTripBackendProject.Outbox;
using Microsoft.EntityFrameworkCore;

namespace DriverTripBackendProject.Data
{
    public class AppDbContext : DbContext

    {

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Area> Areas { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // One-to-many: City → Areas
            modelBuilder.Entity<City>()
                .HasMany(c => c.Areas)
                .WithOne(a => a.City)
                .HasForeignKey(a => a.CityId);

            // One-to-one: Driver → Vehicle
            modelBuilder.Entity<Driver>()
                .HasOne(d => d.Vehicle)
                .WithOne(v => v.Driver)
                .HasForeignKey<Vehicle>(v => v.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Nullable email column (max 256). Nullable keeps existing driver rows valid.
            modelBuilder.Entity<Driver>()
                .Property(d => d.Email)
                .HasMaxLength(256);

            // One-to-many: Driver → Trips
            modelBuilder.Entity<Driver>()
                .HasMany(d => d.Trips)
                .WithOne(t => t.Driver)
                .HasForeignKey(t => t.DriverId);

            // One-to-many: Vehicle → Trips
            modelBuilder.Entity<Vehicle>()
                .HasMany<Trip>()
                .WithOne(t => t.Vehicle)
                .HasForeignKey(t => t.VehicleId);

            // Trip Location Relationships
            modelBuilder.Entity<Trip>()
                .HasOne(t => t.OriginCity)
                .WithMany()
                .HasForeignKey(t => t.OriginCityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.OriginArea)
                .WithMany()
                .HasForeignKey(t => t.OriginAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.DestinationCity)
                .WithMany()
                .HasForeignKey(t => t.DestinationCityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.DestinationArea)
                .WithMany()
                .HasForeignKey(t => t.DestinationAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Outbox: the relay repeatedly queries WHERE ProcessedOnUtc IS NULL,
            // so we index that column for efficient polling.
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("OutboxMessages");
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Type).IsRequired().HasMaxLength(200);
                entity.Property(m => m.Content).IsRequired();
                entity.HasIndex(m => m.ProcessedOnUtc);
            });
        }
    }
}
