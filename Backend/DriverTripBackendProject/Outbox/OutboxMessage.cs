namespace DriverTripBackendProject.Outbox
{
    /// <summary>
    /// A durable "to-be-published" event, stored in the same database (and, when
    /// written by a service, the same transaction) as the business data that
    /// produced it. A background relay (Phase 3) will read unprocessed rows and
    /// publish them to RabbitMQ, then stamp <see cref="ProcessedOnUtc"/>.
    ///
    /// This is the heart of the Transactional Outbox pattern: it lets us commit
    /// the trip and the intent-to-publish atomically, so we never emit a phantom
    /// event (trip rolled back) or lose an event (trip committed but not published).
    /// </summary>
    public class OutboxMessage
    {
        /// <summary>Unique id of this message; also used by consumers for idempotency.</summary>
        public Guid Id { get; set; }

        /// <summary>Event type name, e.g. "TripAssigned" (drives the RabbitMQ routing key later).</summary>
        public string Type { get; set; } = default!;

        /// <summary>Serialized event payload (JSON).</summary>
        public string Content { get; set; } = default!;

        /// <summary>When the business event actually occurred (UTC).</summary>
        public DateTime OccurredOnUtc { get; set; }

        /// <summary>Null until successfully published; set by the relay (UTC).</summary>
        public DateTime? ProcessedOnUtc { get; set; }

        /// <summary>Last publish error, if any (for diagnostics / dead-lettering).</summary>
        public string? Error { get; set; }

        /// <summary>Number of publish attempts made by the relay.</summary>
        public int RetryCount { get; set; }

        /// <summary>Trace id linking this message back to the originating request.</summary>
        public string? CorrelationId { get; set; }
    }
}
