using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Domain.Abstractions;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Shared;

public static class OwnershipExtensions
{
    /// <summary>
    /// Loads an active, tracked entity owned by <paramref name="userId"/>: NotFound when it is missing
    /// or not active, Forbidden when someone else owns it.
    /// </summary>
    public static async Task<ErrorOr<T>> FindOwnedAsync<T>(
        this DbSet<T> set, Guid id, long userId, CancellationToken cancellationToken)
        where T : class, IOwnedEntity, IHasLifeCycle
    {
        var entity = await set.SingleOrDefaultAsync(
            e => e.Id == id && e.LifeCycleStatus == LifeCycleStatus.Active, cancellationToken);

        if (entity == null)
            return AppErrors.NotFound(typeof(T).Name, id);

        return entity.OwnerUserId == userId
            ? entity
            : AppErrors.Forbidden(typeof(T).Name, id);
    }
}
