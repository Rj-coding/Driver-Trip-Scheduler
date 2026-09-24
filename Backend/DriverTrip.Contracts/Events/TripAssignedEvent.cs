namespace DriverTripBackendProject.Events
{
    /// <summary>
    /// The fact that a trip was successfully assigned. A flat, denormalized snapshot
    /// (names as well as ids) so consumers — e.g. a Notification service — can act on
    /// it without calling back into the API.
    ///
    /// Lives in the shared DriverTrip.Contracts library. Intentionally NOT the EF
    /// Trip entity (that would leak persistence details and re-introduce the
    /// Driver/Vehicle serialization cycle).
    /// </summary>
    public sealed class TripAssignedEvent
    {
        public int TripId { get; init; }

        public int DriverId { get; init; }
        public string DriverName { get; init; } = default!;

        public int VehicleId { get; init; }
        public string VehicleNumber { get; init; } = default!;

        public int OriginCityId { get; init; }
        public string OriginCityName { get; init; } = default!;
        public int OriginAreaId { get; init; }
        public string OriginAreaName { get; init; } = default!;

        public int DestinationCityId { get; init; }
        public string DestinationCityName { get; init; } = default!;
        public int DestinationAreaId { get; init; }
        public string DestinationAreaName { get; init; } = default!;

        public DateTime TripStartTime { get; init; }
        public DateTime TripEndTime { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
