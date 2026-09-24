namespace DriverTrip.NotificationService
{
    /// <summary>
    /// RabbitMQ connection settings for the Notification service, bound from the
    /// "RabbitMq" section of appsettings.json. Defaults match the local Docker broker
    /// so the service also works without a config file.
    /// </summary>
    public class RabbitMqOptions
    {
        public const string SectionName = "RabbitMq";

        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "dts";
        public string Password { get; set; } = "dts_password";
        public string VirtualHost { get; set; } = "/";
    }
}
