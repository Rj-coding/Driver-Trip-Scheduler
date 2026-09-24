namespace DriverTrip.NotificationService.Models
{
    /// <summary>
    /// Idempotency record: one row per event this consumer has successfully processed.
    /// The dedup key is (ConsumerName, EventId) so the SAME event can still be processed
    /// once by each different consumer (e.g. notification + analytics) in later phases,
    /// while a redelivery to THIS consumer is recognised and skipped.
    /// </summary>
    public sealed class ProcessedEvent
    {
        /// <summary>Logical name of the consumer that handled the event.</summary>
        public required string ConsumerName { get; set; }

        /// <summary>The source-assigned event id carried in the EventEnvelope.</summary>
        public Guid EventId { get; set; }

        /// <summary>Event type name, stored for observability/debugging.</summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>When this consumer finished processing the event (UTC).</summary>
        public DateTime ProcessedOnUtc { get; set; }
    }
}
