using System.Text;
using System.Text.Json;
using DriverTrip.NotificationService.Data;
using DriverTrip.NotificationService.Models;
using DriverTripBackendProject.Events;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DriverTrip.NotificationService.Consumers
{
    /// <summary>
    /// Consumes TripAssigned events from RabbitMQ.
    ///
    /// It declares the "trip.events" topic exchange, its own durable queue
    /// "q.notify.trip-assigned", a retry queue (delayed redelivery) and a dead-letter
    /// queue. Processing is IDEMPOTENT: each successfully handled EventId is recorded in
    /// the ProcessedEvents table, so a redelivered (duplicate) message is recognised and
    /// skipped. Transient failures are retried up to 3 times (10s apart) then
    /// dead-lettered; malformed messages are dead-lettered immediately.
    /// </summary>
    public sealed class TripAssignedConsumer : BackgroundService
    {
        private const string ExchangeName = "trip.events";
        private const string QueueName = "q.notify.trip-assigned";
        private const string RoutingKey = "trip.assigned";

        // Logical consumer name used as part of the idempotency key (ConsumerName, EventId).
        private const string ConsumerName = QueueName;

        // Dead-letter topology: rejected messages go here instead of being dropped.
        private const string DeadLetterExchangeName = "notify.dlx";
        private const string DeadLetterQueueName = "q.notify.trip-assigned.dlq";

        // Retry topology: transient failures wait here (TTL) then return to the main queue.
        private const string RetryQueueName = "q.notify.trip-assigned.retry";
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 10_000;
        private const string RetryCountHeader = "x-retry-count";
        private const string FailureReasonHeader = "x-failure-reason";

        private readonly RabbitMqOptions _options;
        private readonly ILogger<TripAssignedConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private IConnection? _connection;
        private IChannel? _channel;

        public TripAssignedConsumer(
            IOptions<RabbitMqOptions> options,
            ILogger<TripAssignedConsumer> logger,
            IServiceScopeFactory scopeFactory)
        {
            _options = options.Value;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                ClientProvidedName = "DriverTrip.NotificationService"
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // Declare the SAME topic exchange the producer publishes to (idempotent).
            await _channel.ExchangeDeclareAsync(
                exchange: ExchangeName, type: ExchangeType.Topic,
                durable: true, autoDelete: false, cancellationToken: stoppingToken);

            // Dead-letter topology: a fanout exchange + queue that captures messages
            // this consumer rejects, so failed events are never silently dropped.
            await _channel.ExchangeDeclareAsync(
                exchange: DeadLetterExchangeName, type: ExchangeType.Fanout,
                durable: true, autoDelete: false, cancellationToken: stoppingToken);
            await _channel.QueueDeclareAsync(
                queue: DeadLetterQueueName, durable: true, exclusive: false, autoDelete: false,
                cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(
                queue: DeadLetterQueueName, exchange: DeadLetterExchangeName, routingKey: string.Empty,
                cancellationToken: stoppingToken);

            // Our own durable queue, bound to the exchange for "trip.assigned". The
            // x-dead-letter-exchange argument routes rejected messages to the DLX above.
            var mainQueueArgs = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = DeadLetterExchangeName
            };
            await _channel.QueueDeclareAsync(
                queue: QueueName, durable: true, exclusive: false, autoDelete: false,
                arguments: mainQueueArgs, cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(
                queue: QueueName, exchange: ExchangeName, routingKey: RoutingKey,
                cancellationToken: stoppingToken);

            // Retry queue: a transient failure is parked here for RetryDelayMs, then the
            // TTL expires and RabbitMQ dead-letters it (via the default exchange with
            // routing key = the main queue's name) straight back to the main queue.
            var retryQueueArgs = new Dictionary<string, object?>
            {
                ["x-message-ttl"] = RetryDelayMs,
                ["x-dead-letter-exchange"] = string.Empty, // default exchange
                ["x-dead-letter-routing-key"] = QueueName   // route back to the main queue
            };
            await _channel.QueueDeclareAsync(
                queue: RetryQueueName, durable: true, exclusive: false, autoDelete: false,
                arguments: retryQueueArgs, cancellationToken: stoppingToken);

            // Fair dispatch: don't hand us a new message until we've acked the last one.
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, stoppingToken);

            _logger.LogInformation(
                "Notification consumer ready. Queue '{Queue}' bound to '{Exchange}' ('{RoutingKey}'); " +
                "transient failures retry up to {Max}x every {Delay}ms via '{Retry}', then dead-letter to '{Dlq}'.",
                QueueName, ExchangeName, RoutingKey, MaxRetries, RetryDelayMs, RetryQueueName, DeadLetterQueueName);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceivedAsync;

            await _channel.BasicConsumeAsync(
                queue: QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

            // Keep the background service alive until the app shuts down.
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            // 1) Deserialize. A malformed body is a PERMANENT failure -> straight to DLQ.
            EventEnvelope<TripAssignedEvent>? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<EventEnvelope<TripAssignedEvent>>(json);
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                _logger.LogError(ex, "Malformed message; dead-lettering immediately (no retry). Body: {Body}", json);
                await PublishToDeadLetterAsync(ea, body, "Malformed JSON: " + ex.Message);
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            if (envelope?.Data is null)
            {
                _logger.LogWarning("Message has no TripAssigned data; dead-lettering immediately (no retry). Body: {Body}", json);
                await PublishToDeadLetterAsync(ea, body, "Missing event data");
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            // 2) Idempotency pre-check: if we've already processed this EventId, this is a
            // redelivery/duplicate -> log, ack, and skip re-processing.
            if (await IsAlreadyProcessedAsync(envelope.EventId))
            {
                _logger.LogInformation(
                    "Duplicate EventId {EventId} already processed; skipping and acking.", envelope.EventId);
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            // 3) Process. A processing exception is treated as TRANSIENT -> retry/backoff.
            try
            {
                await ProcessAsync(envelope);
                await RecordProcessedAsync(envelope);
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyViolation(ex))
            {
                // A concurrent delivery recorded the same EventId between our pre-check and
                // insert. The event IS processed, so treat as a duplicate: log and ack.
                _logger.LogInformation(
                    "Duplicate EventId {EventId} detected on insert; skipping and acking.", envelope.EventId);
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                var retryCount = GetRetryCount(ea);
                if (retryCount < MaxRetries)
                {
                    _logger.LogWarning(ex,
                        "Transient failure for EventId {EventId}; scheduling retry {Next}/{Max} in {Delay}ms.",
                        envelope.EventId, retryCount + 1, MaxRetries, RetryDelayMs);
                    await PublishToRetryAsync(ea, body, retryCount + 1);
                }
                else
                {
                    _logger.LogError(ex,
                        "EventId {EventId} still failing after {Max} retries; dead-lettering.",
                        envelope.EventId, MaxRetries);
                    await PublishToDeadLetterAsync(ea, body, $"Failed after {MaxRetries} retries: {ex.Message}");
                }

                // ACK the original: it has been moved to the retry queue or the DLQ, so
                // it must leave the main queue (which therefore never blocks).
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
        }

        private Task ProcessAsync(EventEnvelope<TripAssignedEvent> envelope)
        {
            var trip = envelope.Data;

            // TEMPORARY TEST HOOK (remove when real email/SMS notification is added):
            // lets us exercise the transient-retry path until real processing exists.
            if (string.Equals(trip.DriverName, "__FAIL_TRANSIENT__", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Simulated transient notification failure.");
            }

            _logger.LogInformation(
                "TripAssigned received (EventId={EventId}, CorrelationId={CorrelationId})\n" +
                "  TripId   : {TripId}\n" +
                "  Driver   : {DriverName} (#{DriverId})\n" +
                "  Vehicle  : {VehicleNumber} (#{VehicleId})\n" +
                "  From     : {OriginCity} / {OriginArea}\n" +
                "  To       : {DestCity} / {DestArea}\n" +
                "  Schedule : {Start:o} -> {End:o}",
                envelope.EventId, envelope.CorrelationId,
                trip.TripId,
                trip.DriverName, trip.DriverId,
                trip.VehicleNumber, trip.VehicleId,
                trip.OriginCityName, trip.OriginAreaName,
                trip.DestinationCityName, trip.DestinationAreaName,
                trip.TripStartTime, trip.TripEndTime);

            return Task.CompletedTask;
        }

        /// <summary>Returns true if this consumer has already recorded the given EventId.</summary>
        private async Task<bool> IsAlreadyProcessedAsync(Guid eventId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            return await db.ProcessedEvents
                .AsNoTracking()
                .AnyAsync(e => e.ConsumerName == ConsumerName && e.EventId == eventId);
        }

        /// <summary>Records the EventId so future redeliveries are recognised as duplicates.</summary>
        private async Task RecordProcessedAsync(EventEnvelope<TripAssignedEvent> envelope)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            db.ProcessedEvents.Add(new ProcessedEvent
            {
                ConsumerName = ConsumerName,
                EventId = envelope.EventId,
                EventType = envelope.EventType,
                ProcessedOnUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        /// <summary>True when the DB error is a primary-key / unique-index violation.</summary>
        private static bool IsDuplicateKeyViolation(DbUpdateException ex)
            => ex.InnerException is SqlException sql && (sql.Number == 2627 || sql.Number == 2601);

        private static int GetRetryCount(BasicDeliverEventArgs ea)
        {
            if (ea.BasicProperties.Headers is { } headers
                && headers.TryGetValue(RetryCountHeader, out var value) && value is not null)
            {
                return Convert.ToInt32(value);
            }
            return 0;
        }

        private async Task PublishToRetryAsync(BasicDeliverEventArgs ea, byte[] body, int nextRetryCount)
        {
            var props = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Type = ea.BasicProperties.Type,
                MessageId = ea.BasicProperties.MessageId,
                CorrelationId = ea.BasicProperties.CorrelationId,
                Headers = new Dictionary<string, object?> { [RetryCountHeader] = nextRetryCount }
            };

            // Publish to the retry queue via the default exchange (routingKey = queue name).
            await _channel!.BasicPublishAsync(
                exchange: string.Empty, routingKey: RetryQueueName,
                mandatory: false, basicProperties: props, body: body);
        }

        private async Task PublishToDeadLetterAsync(BasicDeliverEventArgs ea, byte[] body, string reason)
        {
            var props = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Type = ea.BasicProperties.Type,
                MessageId = ea.BasicProperties.MessageId,
                CorrelationId = ea.BasicProperties.CorrelationId,
                Headers = new Dictionary<string, object?>
                {
                    [RetryCountHeader] = GetRetryCount(ea),
                    [FailureReasonHeader] = reason
                }
            };

            await _channel!.BasicPublishAsync(
                exchange: DeadLetterExchangeName, routingKey: string.Empty,
                mandatory: false, basicProperties: props, body: body);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel is not null)
            {
                try { await _channel.CloseAsync(cancellationToken); } catch { /* best effort */ }
                await _channel.DisposeAsync();
            }
            if (_connection is not null)
            {
                try { await _connection.CloseAsync(cancellationToken); } catch { /* best effort */ }
                await _connection.DisposeAsync();
            }
            await base.StopAsync(cancellationToken);
        }
    }
}
