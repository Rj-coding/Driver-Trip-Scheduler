using RabbitMQ.Client;

namespace DriverTripBackendProject.Messaging
{
    /// <summary>
    /// Abstraction over a single, shared RabbitMQ connection.
    /// The rest of the app depends on this interface (Dependency Inversion), so
    /// producers/consumers and tests never touch the concrete client directly.
    /// A connection is expensive and long-lived; channels (created later) are cheap.
    /// </summary>
    public interface IRabbitMqConnection : IAsyncDisposable
    {
        /// <summary>True when an open connection to the broker currently exists.</summary>
        bool IsConnected { get; }

        /// <summary>
        /// Returns the shared connection, opening it lazily on first use.
        /// Safe to call concurrently.
        /// </summary>
        Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
    }
}
