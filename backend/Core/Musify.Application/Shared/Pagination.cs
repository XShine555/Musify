using Microsoft.EntityFrameworkCore;

namespace Musify.Application.Shared;

public record PageRequest(int PageNumber = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
}

public record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int PageCount,
    int TotalItemCount,
    bool HasNextPage,
    bool HasPreviousPage);

public static class PaginationExtensions
{
    /// <summary>Counts and pages the same (already filtered, ordered and projected) query.</summary>
    public static async Task<PaginatedResponse<T>> ToPaginatedAsync<T>(
        this IQueryable<T> query, PageRequest page, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(page.PageNumber, 1);
        var pageSize = Math.Clamp(page.PageSize, 1, PageRequest.MaxPageSize);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PaginatedResponse<T>(
            items,
            pageNumber,
            pageSize,
            pageCount,
            totalCount,
            HasNextPage: pageNumber < pageCount,
            HasPreviousPage: pageNumber > 1 && pageCount > 0);
    }
}
