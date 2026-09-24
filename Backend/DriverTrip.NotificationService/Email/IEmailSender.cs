namespace DriverTrip.NotificationService.Email
{
    /// <summary>Sends a plain-text email. Implementations decide transport (SMTP).</summary>
    public interface IEmailSender
    {
        /// <summary>
        /// Sends a plain-text email. Throws <see cref="PermanentEmailException"/> for
        /// non-retryable failures (auth/config); other exceptions indicate transient
        /// failures the caller may retry.
        /// </summary>
        Task SendAsync(string toAddress, string toName, string subject, string body, CancellationToken cancellationToken = default);
    }
}
