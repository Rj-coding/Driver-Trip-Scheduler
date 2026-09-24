using DriverTripBackendProject.Data;
using DriverTripBackendProject.Messaging;
using Microsoft.EntityFrameworkCore;

namespace DriverTripBackendProject.Outbox
{
    /// <summary>
    /// Background worker that reliably delivers outbox messages to RabbitMQ.
    ///
    /// Every <see cref="PollInterval"/> it reads a batch of unprocessed rows,
    /// publishes each to the trip events exchange, and stamps <c>ProcessedOnUtc</c>
    /// only after the broker confirms the publish. If a publish fails (e.g. the
    /// broker is down) the row is left unprocessed with an incremented retry count
    /// and picked up again on the next tick — giving at-least-once delivery.
    ///
    /// This is a singleton hosted service, so it opens a DI scope per iteration to
    /// resolve the Scoped <see cref="AppDbContext"/>.
    /// </summary>
    public sealed class OutboxRelay : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
        private const int BatchSize = 20;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEventPublisher _publisher;
        private readonly ILogger<OutboxRelay> _logger;

        public OutboxRelay(
            IServiceScopeFactory scopeFactory,
            IEventPublisher publisher,
            ILogger<OutboxRelay> logger)
        {
            _scopeFactory = scopeFactory;
            _publisher = publisher;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox relay started (poll every {Seconds}s).", PollInterval.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // Never let one bad iteration kill the loop.
                    _logger.LogError(ex, "Outbox relay iteration failed; will retry next tick.");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Normal on shutdown.
                }
            }

            _logger.LogInformation("Outbox relay stopping.");
        }

        private async Task ProcessOutboxAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var messages = await db.OutboxMessages
                .Where(m => m.ProcessedOnUtc == null)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (messages.Count == 0)
            {
                return;
            }

            foreach (var message in messages)
            {
                try
                {
                    var routingKey = TripEventRouting.ResolveRoutingKey(message.Type);

                    await _publisher.PublishAsync(
                        routingKey,
                        message.Type,
                        message.Content,
                        message.Id.ToString(),
                        message.CorrelationId,
                        cancellationToken);

                    // Only mark processed AFTER a confirmed publish.
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    message.Error = null;

                    _logger.LogInformation(
                        "Published outbox message {Id} ({Type}) with routing key '{RoutingKey}'.",
                        message.Id, message.Type, routingKey);
                }
                catch (Exception ex)
                {
                    // Leave the row unprocessed so it is retried next tick.
                    message.RetryCount++;
                    message.Error = ex.Message;

                    _logger.LogWarning(ex,
                        "Failed to publish outbox message {Id}; retry count now {RetryCount}.",
                        message.Id, message.RetryCount);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
