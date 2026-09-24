using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DriverTripBackendProject.Messaging
{
    /// <summary>
    /// Owns ONE long-lived RabbitMQ connection for the whole application
    /// (registered as a Singleton). The connection is created lazily on first
    /// use and reused thereafter. If the broker is unavailable, callers get an
    /// exception — the app itself still boots and serves requests, because
    /// nothing opens this connection during startup.
    /// </summary>
    public sealed class RabbitMqConnection : IRabbitMqConnection
    {
        private readonly ConnectionFactory _factory;
        private readonly ILogger<RabbitMqConnection> _logger;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);
        private IConnection? _connection;

        public RabbitMqConnection(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnection> logger)
        {
            var o = options.Value;
            _logger = logger;
            _factory = new ConnectionFactory
            {
                HostName = o.HostName,
                Port = o.Port,
                UserName = o.UserName,
                Password = o.Password,
                VirtualHost = o.VirtualHost,
                // Let the client transparently recover if the broker blips.
                AutomaticRecoveryEnabled = true,
                // A friendly name shown in the RabbitMQ management UI.
                ClientProvidedName = "DriverTripScheduler.Api"
            };
        }

        public bool IsConnected => _connection is { IsOpen: true };

        public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (IsConnected)
            {
                return _connection!;
            }

            await _connectionLock.WaitAsync(cancellationToken);
            try
            {
                // Double-check: another thread may have connected while we waited.
                if (IsConnected)
                {
                    return _connection!;
                }

                _logger.LogInformation(
                    "Opening RabbitMQ connection to {Host}:{Port} (vhost '{VHost}')",
                    _factory.HostName, _factory.Port, _factory.VirtualHost);

                _connection = await _factory.CreateConnectionAsync(cancellationToken);

                _logger.LogInformation("RabbitMQ connection established.");
                return _connection;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection is not null)
            {
                try
                {
                    await _connection.CloseAsync();
                    await _connection.DisposeAsync();
                }
                catch
                {
                    // Best-effort cleanup on shutdown; nothing actionable here.
                }
            }

            _connectionLock.Dispose();
        }
    }
}
