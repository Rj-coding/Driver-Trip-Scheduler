using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DriverTrip.NotificationService.Email
{
    /// <summary>
    /// MailKit-based SMTP sender (Gmail: smtp.gmail.com:587 STARTTLS, App Password auth).
    /// A fresh <see cref="SmtpClient"/> is created per send because MailKit's client is
    /// not thread-safe and this consumer may run concurrently in future.
    ///
    /// Failure classification (see <see cref="PermanentEmailException"/>): authentication,
    /// TLS-handshake, and missing-credential errors are PERMANENT (rethrown as
    /// <see cref="PermanentEmailException"/> so the consumer dead-letters immediately).
    /// Everything else (timeouts, sockets, transient SMTP errors) propagates unchanged so
    /// the consumer's existing 3-retry path handles it.
    /// </summary>
    public sealed class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions _options;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(string toAddress, string toName, string subject, string body, CancellationToken cancellationToken = default)
        {
            // Missing credentials are a configuration error -> permanent (retrying cannot help).
            if (string.IsNullOrWhiteSpace(_options.User) || string.IsNullOrWhiteSpace(_options.Password))
            {
                throw new PermanentEmailException(
                    "SMTP credentials are not configured. Set Email:User and Email:Password via user-secrets or environment variables.");
            }

            var fromAddress = string.IsNullOrWhiteSpace(_options.FromAddress) ? _options.User : _options.FromAddress;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, fromAddress));
            message.To.Add(new MailboxAddress(toName, toAddress));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls, cancellationToken);
                await client.AuthenticateAsync(_options.User, _options.Password, cancellationToken);
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(quit: true, cancellationToken);
            }
            catch (AuthenticationException ex)
            {
                // Wrong App Password / account: retrying with the same creds is futile -> permanent.
                throw new PermanentEmailException("SMTP authentication failed (check the Gmail App Password).", ex);
            }
            catch (SslHandshakeException ex)
            {
                // TLS misconfiguration -> permanent.
                throw new PermanentEmailException("SMTP TLS/SSL handshake failed (configuration).", ex);
            }
            // All other exceptions (SmtpCommandException transient codes, timeouts, IO/socket
            // errors) intentionally propagate so the consumer treats them as TRANSIENT.
        }
    }
}
