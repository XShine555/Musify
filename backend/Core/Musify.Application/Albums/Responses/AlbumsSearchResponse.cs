namespace Musify.Application.Albums.Responses
{

    public record AlbumsSearchResponse(
        IReadOnlyList<AlbumSearchItemResponse> Items,
        int PageNumber,
        int PageSize,
        int PageCount,
        int TotalItemCount,
        bool HasPreviousPage,
        bool HasNextPage);
}
