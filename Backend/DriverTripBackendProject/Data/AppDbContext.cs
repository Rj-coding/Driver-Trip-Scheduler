using DriverTripBackendProject.Models;
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
        }
    }
}
