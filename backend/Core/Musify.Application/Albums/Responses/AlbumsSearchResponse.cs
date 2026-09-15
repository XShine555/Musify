namespace Musify.Application.Albums.Responses
{
    /// <summary>A page of the user's catalog albums (their own).</summary>
    public record AlbumsSearchResponse(
        IReadOnlyList<AlbumSearchItemResponse> Items,
        int PageNumber,
        int PageSize,
        int PageCount,
        int TotalItemCount,
        bool HasPreviousPage,
        bool HasNextPage);
}
