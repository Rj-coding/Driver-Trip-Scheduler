using Microsoft.EntityFrameworkCore.Storage;

namespace DriverTripBackendProject.Data
{
    /// <summary>
    /// Default <see cref="IUnitOfWork"/> backed by the Scoped <see cref="AppDbContext"/>.
    /// Because it shares the same context instance as the repositories and the outbox
    /// writer within a request scope, a transaction started here spans all of them.
    /// </summary>
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => _context.Database.BeginTransactionAsync(cancellationToken);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => _context.SaveChangesAsync(cancellationToken);
    }
}
