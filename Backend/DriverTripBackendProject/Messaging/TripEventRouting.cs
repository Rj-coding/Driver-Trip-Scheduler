namespace DriverTripBackendProject.Messaging
{
    /// <summary>
    /// Single source of truth for the trip events exchange and the mapping from an
    /// event type name to its RabbitMQ routing key. A topic exchange uses these
    /// keys so different consumers can subscribe to just the events they care about
    /// (e.g. a notifier binds "trip.assigned", analytics binds "trip.#").
    /// </summary>
    public static class TripEventRouting
    {
        // TODO: This topic exchange now carries trip AND driver events. Once more
        // aggregates publish here, rename it to an aggregate-neutral name such as
        // "domain.events" (requires a one-off RabbitMQ topology migration + re-bind).
        /// <summary>The topic exchange all domain events are published to.</summary>
        public const string ExchangeName = "trip.events";

        /// <summary>Maps an event type (e.g. "TripAssignedEvent") to its routing key.</summary>
        public static string ResolveRoutingKey(string eventType) => eventType switch
        {
            "TripAssignedEvent" => "trip.assigned",
            "TripRescheduledEvent" => "trip.rescheduled",
            "TripCancelledEvent" => "trip.cancelled",
            "DriverCreatedEvent" => "driver.created",
            _ => "trip.unknown"
        };
    }
}
