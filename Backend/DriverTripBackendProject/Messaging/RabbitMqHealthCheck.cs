using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DriverTripBackendProject.Messaging
{
    /// <summary>
    /// Reports RabbitMQ connectivity at the /health endpoint.
    /// A down broker surfaces as "Unhealthy" — it never throws out of the
    /// pipeline or prevents the application from running.
    /// </summary>
    public sealed class RabbitMqHealthCheck : IHealthCheck
    {
        private readonly IRabbitMqConnection _connection;

        public RabbitMqHealthCheck(IRabbitMqConnection connection)
        {
            _connection = connection;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var connection = await _connection.GetConnectionAsync(cancellationToken);
                return connection.IsOpen
                    ? HealthCheckResult.Healthy("RabbitMQ connection is open.")
                    : HealthCheckResult.Unhealthy("RabbitMQ connection is not open.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Could not connect to RabbitMQ.", ex);
            }
        }
    }
}
