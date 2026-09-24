using DriverTrip.NotificationService;
using DriverTrip.NotificationService.Consumers;
using DriverTrip.NotificationService.Data;
using DriverTrip.NotificationService.Email;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Load SMTP credentials (Email:User / Email:Password) from User Secrets in any
// environment (the worker runs as Production by default, where secrets are not loaded
// automatically). Environment variables (Email__User / Email__Password) also work via
// the default configuration providers.
builder.Configuration.AddUserSecrets(typeof(RabbitMqOptions).Assembly, optional: true);

// Bind RabbitMQ settings and start the consumer as a hosted background service.
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));

// Email: bind SMTP options and register the MailKit-based sender (D4).
builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

// Idempotency store: the consumer records each processed EventId here so redelivered
// (duplicate) messages are recognised and skipped. Lives in the shared DriverTripDBKafka
// but uses its own migrations history table (see NotificationDbContext).
builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DriverTripDB"),
        sql => sql.MigrationsHistoryTable(NotificationDbContext.MigrationsHistoryTable)));

builder.Services.AddHostedService<TripAssignedConsumer>();

// D3: a second consumer for DriverCreated events. It reuses the same reliability model
// (shared exchange, retry/DLQ pattern, shared ProcessedEvents idempotency table) and does
// NOT touch TripAssignedConsumer. Logging only — real email is milestone D4.
builder.Services.AddHostedService<DriverCreatedConsumer>();

var host = builder.Build();
host.Run();
