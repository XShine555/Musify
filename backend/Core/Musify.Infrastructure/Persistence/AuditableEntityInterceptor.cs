using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Musify.Domain.Abstractions;

namespace Musify.Infrastructure.Persistence
{
    public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            ApplyTimestamps(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            ApplyTimestamps(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private static void ApplyTimestamps(DbContext? context)
        {
            if (context == null)
                return;

            // SavingChanges(Async) fires before EF's own automatic change detection for this
            // save, so entities modified through plain property assignment (query, then set a
            // property, then SaveChanges — the normal case) still show as Unchanged here unless
            // something already forced detection. Force it explicitly, otherwise UpdatedAt is
            // never bumped on a real update, only on inserts.
            context.ChangeTracker.DetectChanges();

            var now = DateTime.UtcNow;

            foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = now;
                }
            }
        }
    }
}
