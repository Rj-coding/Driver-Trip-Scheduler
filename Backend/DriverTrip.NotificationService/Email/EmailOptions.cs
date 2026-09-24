namespace DriverTrip.NotificationService.Email
{
    /// <summary>
    /// SMTP settings for sending notification emails, bound from the "Email" section.
    ///
    /// SECURITY: only non-secret values (host, port, from-name) belong in appsettings.json.
    /// The Gmail account (User) and the App Password (Password) MUST come from .NET User
    /// Secrets (development) or environment variables (Email__User / Email__Password) —
    /// never committed to source control.
    /// </summary>
    public sealed class EmailOptions
    {
        public const string SectionName = "Email";

        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587; // STARTTLS

        /// <summary>Gmail address used to authenticate and as the From address.</summary>
        public string User { get; set; } = string.Empty;

        /// <summary>Gmail App Password (16 chars). Loaded from secrets/env, never appsettings.</summary>
        public string Password { get; set; } = string.Empty;

        public string FromName { get; set; } = "Driver Trip Scheduler";

        /// <summary>Optional explicit From address; defaults to <see cref="User"/> when empty.</summary>
        public string FromAddress { get; set; } = string.Empty;
    }
}
