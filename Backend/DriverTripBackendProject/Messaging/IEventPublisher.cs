namespace DriverTripBackendProject.Messaging
{
    /// <summary>
    /// Publishes a single event (already serialized as JSON) to the trip events
    /// exchange. Implementations use publisher confirmations so that the returned
    /// task only completes once the broker has acknowledged the message — which is
    /// what lets the relay safely mark an outbox row as processed.
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Publishes <paramref name="jsonBody"/> to the trip events exchange using
        /// the given routing key. Completes only after the broker confirms receipt.
        /// </summary>
        Task PublishAsync(
            string routingKey,
            string eventType,
            string jsonBody,
            string? messageId,
            string? correlationId,
            CancellationToken cancellationToken = default);
    }
}
