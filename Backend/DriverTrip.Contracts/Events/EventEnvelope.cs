namespace DriverTripBackendProject.Events
{
    /// <summary>
    /// A standard wrapper around every integration event we publish. It carries
    /// metadata (who/when/which version + a correlation id for tracing) alongside
    /// the strongly-typed <typeparamref name="T"/> payload.
    ///
    /// Lives in the shared DriverTrip.Contracts library so both the producer (API)
    /// and consumers (e.g. the Notification service) use the exact same shape.
    /// </summary>
    /// <typeparam name="T">The event payload type, e.g. TripAssignedEvent.</typeparam>
    public sealed class EventEnvelope<T>
    {
        /// <summary>Unique id of this event; consumers use it for idempotency.</summary>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <summary>Event type name (defaults to the payload type name), e.g. "TripAssignedEvent".</summary>
        public string EventType { get; init; } = typeof(T).Name;

        /// <summary>Schema version of the payload, so it can evolve without breaking consumers.</summary>
        public int Version { get; init; } = 1;

        /// <summary>When the business event occurred (UTC).</summary>
        public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;

        /// <summary>Trace id linking this event back to the originating request.</summary>
        public string? CorrelationId { get; init; }

        /// <summary>The actual event data.</summary>
        public required T Data { get; init; }
    }
}
