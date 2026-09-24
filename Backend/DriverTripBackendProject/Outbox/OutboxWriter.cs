using System.Text.Json;
using DriverTripBackendProject.Data;
using DriverTripBackendProject.Events;

namespace DriverTripBackendProject.Outbox
{
    /// <summary>
    /// Default <see cref="IOutboxWriter"/> that turns an event into an
    /// <see cref="OutboxMessage"/> and stages it on the injected <see cref="AppDbContext"/>.
    ///
    /// Registered as Scoped so it shares the SAME DbContext instance as the service
    /// performing the business SaveChanges — that shared context is what makes the
    /// trip and the outbox row commit in one atomic transaction.
    /// </summary>
    public sealed class OutboxWriter : IOutboxWriter
    {
        private readonly AppDbContext _context;

        public OutboxWriter(AppDbContext context)
        {
            _context = context;
        }

        public void Add<T>(T data, string? correlationId = null)
        {
            var envelope = new EventEnvelope<T>
            {
                CorrelationId = correlationId,
                Data = data
            };

            var message = new OutboxMessage
            {
                Id = envelope.EventId,
                Type = envelope.EventType,
                Content = JsonSerializer.Serialize(envelope),
                OccurredOnUtc = envelope.OccurredOnUtc,
                CorrelationId = correlationId,
                RetryCount = 0
            };

            // Stage only. We deliberately do NOT call SaveChanges here: the caller's
            // transaction commits this row together with the business data, which is
            // the whole reliability guarantee of the outbox pattern.
            _context.OutboxMessages.Add(message);
        }
    }
}
