namespace DriverTripBackendProject.Events
{
    /// <summary>
    /// The fact that a new driver was created. A flat, denormalized snapshot (names
    /// and contact details, not the EF entity) so consumers — e.g. a Notification
    /// service that emails the driver — can act on it without calling back into the API.
    ///
    /// Lives in the shared DriverTrip.Contracts library alongside TripAssignedEvent and
    /// is wrapped in EventEnvelope&lt;T&gt; when written to the outbox / published.
    /// </summary>
    public sealed class DriverCreatedEvent
    {
        public int DriverId { get; init; }
        public string Name { get; init; } = default!;
        public string Email { get; init; } = default!;
        public string PhoneNumber { get; init; } = default!;
        public DateTime CreatedAt { get; init; }
    }
}
