using System.Text;
using RabbitMQ.Client;

namespace DriverTripBackendProject.Messaging
{
    /// <summary>
    /// Publishes events to the "trip.events" topic exchange over the shared
    /// <see cref="IRabbitMqConnection"/>. Uses a single channel with publisher
    /// confirmations enabled, so <see cref="PublishAsync"/> only completes once
    /// the broker has acknowledged the message (at-least-once delivery). Messages
    /// are marked persistent so they survive a broker restart.
    /// </summary>
    public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
    {
        private readonly IRabbitMqConnection _connection;
        private readonly ILogger<RabbitMqEventPublisher> _logger;
        private readonly SemaphoreSlim _channelLock = new(1, 1);
        private IChannel? _channel;

        public RabbitMqEventPublisher(IRabbitMqConnection connection, ILogger<RabbitMqEventPublisher> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task PublishAsync(
            string routingKey,
            string eventType,
            string jsonBody,
            string? messageId,
            string? correlationId,
            CancellationToken cancellationToken = default)
        {
            var channel = await GetChannelAsync(cancellationToken);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Type = eventType
            };
            if (messageId is not null) properties.MessageId = messageId;
            if (correlationId is not null) properties.CorrelationId = correlationId;

            var body = Encoding.UTF8.GetBytes(jsonBody);

            // With publisher confirmations, awaiting this completes only after the
            // broker acknowledges the publish (or throws if it is rejected).
            await channel.BasicPublishAsync(
                exchange: TripEventRouting.ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }

        private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            await _channelLock.WaitAsync(cancellationToken);
            try
            {
                if (_channel is { IsOpen: true })
                {
                    return _channel;
                }

                var connection = await _connection.GetConnectionAsync(cancellationToken);

                _channel = await connection.CreateChannelAsync(
                    new CreateChannelOptions(
                        publisherConfirmationsEnabled: true,
                        publisherConfirmationTrackingEnabled: true),
                    cancellationToken);

                // Idempotently ensure the topic exchange exists (durable so it
                // survives broker restarts).
                await _channel.ExchangeDeclareAsync(
                    exchange: TripEventRouting.ExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "RabbitMQ publisher channel ready; declared topic exchange '{Exchange}'.",
                    TripEventRouting.ExchangeName);

                return _channel;
            }
            finally
            {
                _channelLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null)
            {
                try
                {
                    await _channel.CloseAsync();
                    await _channel.DisposeAsync();
                }
                catch
                {
                    // Best-effort cleanup on shutdown.
                }
            }

            _channelLock.Dispose();
        }
    }
}
