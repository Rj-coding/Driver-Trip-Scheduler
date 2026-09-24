namespace DriverTripBackendProject.Messaging
{
    /// <summary>
    /// Strongly-typed RabbitMQ settings bound from the "RabbitMq" section of
    /// appsettings.json (Options pattern). Keeping these in configuration means
    /// we can point at a different broker per environment without recompiling —
    /// exactly like the SQL Server connection string.
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
