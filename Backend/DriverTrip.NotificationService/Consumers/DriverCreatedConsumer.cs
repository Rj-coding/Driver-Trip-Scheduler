using System.Text;
using System.Text.Json;
using DriverTrip.NotificationService.Data;
using DriverTrip.NotificationService.Email;
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
    /// Consumes DriverCreated events from RabbitMQ.
    ///
    /// This is intentionally a near-duplicate of <see cref="TripAssignedConsumer"/> (D3):
    /// it proves the existing reliability model (shared "trip.events" exchange, retry
    /// queue, dead-letter queue, and the shared ProcessedEvents idempotency table) is
    /// reusable for a second event type WITHOUT touching the production-tested Trip
    /// consumer. Extracting a shared base class is a deliberate later milestone.
    ///
    /// Reliability is identical to the Trip consumer: idempotent (dedupe by EventId under
    /// ConsumerName "q.notify.driver-created"), transient failures retried 3x (10s apart)
    /// then dead-lettered, malformed messages dead-lettered immediately.
    ///
    /// NOTE (D3 topology choice): "notify.dlx" is a FANOUT exchange. Binding this
    /// consumer's DLQ to it and publishing through it would fan every dead-letter out to
    /// BOTH DLQs (cross-contamination). To keep the per-consumer DLQ isolated with zero
    /// changes to the Trip consumer, we reuse notify.dlx as the main queue's configured
    /// x-dead-letter-exchange but publish the actual dead-letter DIRECTLY to this
    /// consumer's own DLQ via the default exchange (same mechanism as the retry publish).
    /// Converting notify.dlx to a routed (direct/topic) exchange is a recommended follow-up.
    ///
    /// This consumer only LOGS (no email / SMTP / MailKit — that is milestone D4).
    /// </summary>
    public sealed class DriverCreatedConsumer : BackgroundService
    {
        private const string ExchangeName = "trip.events";
        private const string QueueName = "q.notify.driver-created";
        private const string RoutingKey = "driver.created";

        // Logical consumer name used as part of the idempotency key (ConsumerName, EventId).
        private const string ConsumerName = QueueName;

        // Dead-letter topology: reuse the shared "notify.dlx" as this queue's configured
        // DLX, but deliver dead-letters directly to our own isolated DLQ (see class note).
        private const string DeadLetterExchangeName = "notify.dlx";
        private const string DeadLetterQueueName = "q.notify.driver-created.dlq";

        // Retry topology: transient failures wait here (TTL) then return to the main queue.
        private const string RetryQueueName = "q.notify.driver-created.retry";
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 10_000;
        private const string RetryCountHeader = "x-retry-count";
        private const string FailureReasonHeader = "x-failure-reason";

        private readonly RabbitMqOptions _options;
        private readonly ILogger<DriverCreatedConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEmailSender _emailSender;

        private IConnection? _connection;
        private IChannel? _channel;

        public DriverCreatedConsumer(
            IOptions<RabbitMqOptions> options,
            ILogger<DriverCreatedConsumer> logger,
            IServiceScopeFactory scopeFactory,
            IEmailSender emailSender)
        {
            _options = options.Value;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _emailSender = emailSender;
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
                ClientProvidedName = "DriverTrip.NotificationService (driver-created)"
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // Declare the SAME shared topic exchange the producer publishes to (idempotent).
            await _channel.ExchangeDeclareAsync(
                exchange: ExchangeName, type: ExchangeType.Topic,
                durable: true, autoDelete: false, cancellationToken: stoppingToken);

            // Reuse the shared dead-letter exchange (fanout, declared idempotently so it
            // matches the Trip consumer's declaration). We do NOT bind our DLQ to it — see
            // the class note: we deliver dead-letters directly to our own DLQ to stay isolated.
            await _channel.ExchangeDeclareAsync(
                exchange: DeadLetterExchangeName, type: ExchangeType.Fanout,
                durable: true, autoDelete: false, cancellationToken: stoppingToken);

            // Our own durable dead-letter queue. Fed directly (default exchange, routingKey =
            // queue name) by PublishToDeadLetterAsync, so it only ever holds driver failures.
            await _channel.QueueDeclareAsync(
                queue: DeadLetterQueueName, durable: true, exclusive: false, autoDelete: false,
                cancellationToken: stoppingToken);

            // Our own durable main queue, bound to the exchange for "driver.created". The
            // x-dead-letter-exchange arg reuses notify.dlx (never fires in practice because
            // we always ack and route failures ourselves).
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
                "DriverCreated consumer ready. Queue '{Queue}' bound to '{Exchange}' ('{RoutingKey}'); " +
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
            EventEnvelope<DriverCreatedEvent>? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<EventEnvelope<DriverCreatedEvent>>(json);
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
                _logger.LogWarning("Message has no DriverCreated data; dead-lettering immediately (no retry). Body: {Body}", json);
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
            catch (PermanentEmailException ex)
            {
                // Non-retryable email failure (bad/missing credentials, TLS/config). Retrying
                // with the same broken config is futile, so dead-letter immediately instead of
                // burning the retry budget. (Reuses the existing DLQ path; retry topology unchanged.)
                _logger.LogError(ex,
                    "Permanent email failure for EventId {EventId}; dead-lettering immediately (no retry).",
                    envelope.EventId);
                await PublishToDeadLetterAsync(ea, body, "Permanent email failure: " + ex.Message);
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

        private async Task ProcessAsync(EventEnvelope<DriverCreatedEvent> envelope)
        {
            var driver = envelope.Data;

            const string subject = "Welcome to Driver Trip Scheduler";
            var body =
                $"Hello {driver.Name},\n\n" +
                "Welcome to Driver Trip Scheduler.\n\n" +
                "Your driver profile has been created successfully.\n\n" +
                "Driver Details\n\n" +
                $"Driver Name:\n{driver.Name}\n\n" +
                $"Phone Number:\n{driver.PhoneNumber}\n\n" +
                "We look forward to working with you.\n\n" +
                "Regards,\n" +
                "Driver Trip Scheduler Team\n";

            // Send to the driver's actual email address carried in the event.
            await _emailSender.SendAsync(driver.Email, driver.Name, subject, body);

            _logger.LogInformation(
                "Welcome email sent for DriverCreated EventId {EventId} (driver '{Name}' #{DriverId}) to {Recipient}.",
                envelope.EventId, driver.Name, driver.DriverId, driver.Email);
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
        private async Task RecordProcessedAsync(EventEnvelope<DriverCreatedEvent> envelope)
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

            // Deliver directly to our OWN dead-letter queue via the default exchange
            // (routingKey = DLQ name). This keeps the driver DLQ isolated from the trip DLQ
            // even though both nominally share the fanout "notify.dlx" (see class note).
            await _channel!.BasicPublishAsync(
                exchange: string.Empty, routingKey: DeadLetterQueueName,
                mandatory: false, basicProperties: props, body: body);
        }
    }
}
