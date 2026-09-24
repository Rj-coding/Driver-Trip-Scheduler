namespace DriverTrip.NotificationService.Email
{
    /// <summary>
    /// Thrown for email failures that are PERMANENT (retrying is futile): bad/missing
    /// credentials, TLS/config errors. The consumer dead-letters these immediately
    /// instead of burning the 3-retry budget, while transient failures (timeouts,
    /// sockets, SMTP 4xx) propagate as ordinary exceptions and follow the retry path.
    /// </summary>
    public sealed class PermanentEmailException : Exception
    {
        public PermanentEmailException(string message) : base(message)
        {
        }

        public PermanentEmailException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
