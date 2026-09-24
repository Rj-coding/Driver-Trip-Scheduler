using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DriverTrip.NotificationService.Data
{
    /// <summary>
    /// Design-time factory used only by "dotnet ef" (migrations add / database update).
    /// It reads the connection string from appsettings.json so the tooling never has to
    /// build the full host (which would start the RabbitMQ consumer).
    /// </summary>
    public sealed class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
    {
        public NotificationDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var connectionString = configuration.GetConnectionString("DriverTripDB");

            var options = new DbContextOptionsBuilder<NotificationDbContext>()
                .UseSqlServer(
                    connectionString,
                    sql => sql.MigrationsHistoryTable(NotificationDbContext.MigrationsHistoryTable))
                .Options;

            return new NotificationDbContext(options);
        }
    }
}
