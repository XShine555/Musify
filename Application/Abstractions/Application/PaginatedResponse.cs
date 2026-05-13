using X.PagedList;

namespace Musify.Application.Abstractions.Application
{
    public record PaginatedResponse<T>(
        IReadOnlyCollection<T> Items,
        int PageNumber,
        int PageSize,
        int PageCount,
        int TotalItemCount,
        bool HasNextPage,
        bool HasPreviousPage)
    {
        public static PaginatedResponse<T> FromPagedList(IPagedList<T> pagedList)
        {
            return new PaginatedResponse<T>(
                pagedList.ToArray(),
                pagedList.PageNumber,
                pagedList.PageSize,
                pagedList.PageCount,
                pagedList.TotalItemCount,
                pagedList.HasNextPage,
                pagedList.HasPreviousPage);
        }
    }
}
