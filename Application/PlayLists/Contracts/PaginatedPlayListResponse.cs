namespace Musify.Application.PlayLists.Contracts
{
    public record PaginatedPlayListResponse(
        IReadOnlyCollection<PlayListResponse> PlayLists,
        int PageNumber,
        int PageSize,
        int PageCount,
        int TotalItemCount,
        bool HasNextPage,
        bool HasPreviousPage
    );
}