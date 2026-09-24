namespace DriverTripBackendProject.Outbox
{
    /// <summary>
    /// Stages an integration event as an outbox row on the CURRENT unit of work.
    ///
    /// Critically, implementations must NOT call SaveChanges. The row is committed
    /// by the caller's transaction, so the business data and the event are written
    /// atomically (both commit or both roll back). A background relay publishes the
    /// row to RabbitMQ later.
    /// </summary>
    public interface IOutboxWriter
    {
        /// <summary>
        /// Serializes <paramref name="data"/> into an outbox row and stages it on the
        /// current DbContext. Does not persist until the caller saves.
        /// </summary>
        /// <typeparam name="T">The event payload type, e.g. TripAssignedEvent.</typeparam>
        /// <param name="data">The event payload.</param>
        /// <param name="correlationId">Optional trace id from the originating request.</param>
        void Add<T>(T data, string? correlationId = null);
    }
}
