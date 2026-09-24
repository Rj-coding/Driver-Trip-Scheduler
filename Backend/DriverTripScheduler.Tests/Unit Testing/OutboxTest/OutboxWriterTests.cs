using DriverTripBackendProject.Data;
using DriverTripBackendProject.Events;
using DriverTripBackendProject.Outbox;
using Microsoft.EntityFrameworkCore;

namespace DriverTripScheduler.Tests.OutboxTest
{
    [TestClass]
    public class OutboxWriterTests
    {
        private static AppDbContext NewInMemoryContext() =>
            new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);

        private static TripAssignedEvent SampleEvent() => new()
        {
            TripId = 42,
            DriverId = 7,
            DriverName = "Jackson",
            VehicleId = 3,
            VehicleNumber = "MH1654",
            OriginCityName = "Mumbai",
            OriginAreaName = "Andheri",
            DestinationCityName = "Delhi",
            DestinationAreaName = "Saket",
            TripStartTime = DateTime.UtcNow.AddHours(2),
            TripEndTime = DateTime.UtcNow.AddHours(4),
            CreatedAt = DateTime.UtcNow
        };

        [TestMethod]
        public void Add_StagesMessage_ButDoesNotPersistUntilSaveChanges()
        {
            using var context = NewInMemoryContext();
            var writer = new OutboxWriter(context);

            writer.Add(SampleEvent(), correlationId: "corr-1");

            // Staged in the change tracker as Added...
            var staged = context.ChangeTracker.Entries<OutboxMessage>()
                .Count(e => e.State == EntityState.Added);
            Assert.AreEqual(1, staged, "message should be staged as Added");

            // ...but NOT yet in the store — proving it commits with the caller's tx.
            Assert.AreEqual(0, context.OutboxMessages.AsNoTracking().Count(),
                "nothing should be persisted before SaveChanges");
        }

        [TestMethod]
        public async Task Add_ThenSaveChanges_PersistsOneMessage_WithMetadata()
        {
            using var context = NewInMemoryContext();
            var writer = new OutboxWriter(context);

            writer.Add(SampleEvent(), correlationId: "corr-1");
            await context.SaveChangesAsync();

            var msg = context.OutboxMessages.Single();
            Assert.AreEqual("TripAssignedEvent", msg.Type);
            Assert.AreEqual("corr-1", msg.CorrelationId);
            Assert.IsNull(msg.ProcessedOnUtc, "a new message must be unprocessed");
            Assert.AreEqual(0, msg.RetryCount);
            Assert.IsFalse(string.IsNullOrWhiteSpace(msg.Content));
        }

        [TestMethod]
        public async Task Add_SerializesEnvelope_ThatDeserializesBackToData()
        {
            using var context = NewInMemoryContext();
            var writer = new OutboxWriter(context);

            writer.Add(SampleEvent(), correlationId: "corr-9");
            await context.SaveChangesAsync();

            var msg = context.OutboxMessages.Single();
            var envelope = System.Text.Json.JsonSerializer
                .Deserialize<EventEnvelope<TripAssignedEvent>>(msg.Content);

            Assert.IsNotNull(envelope);
            Assert.AreEqual("TripAssignedEvent", envelope!.EventType);
            Assert.AreEqual(1, envelope.Version);
            Assert.AreEqual("corr-9", envelope.CorrelationId);
            Assert.AreEqual(42, envelope.Data.TripId);
            Assert.AreEqual("MH1654", envelope.Data.VehicleNumber);
            Assert.AreEqual("Jackson", envelope.Data.DriverName);
        }
    }
}
