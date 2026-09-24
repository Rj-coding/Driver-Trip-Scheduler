using Microsoft.EntityFrameworkCore.Storage;

namespace DriverTripBackendProject.Data
{
    /// <summary>
    /// A minimal unit-of-work seam over <see cref="AppDbContext"/> so a service can
    /// own an explicit transaction that spans multiple saves. This is what lets the
    /// trip row and its outbox row commit atomically (Transactional Outbox pattern).
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>Begins an explicit database transaction on the shared DbContext.</summary>
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>Flushes pending changes. Inside an explicit transaction this does NOT commit.</summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
