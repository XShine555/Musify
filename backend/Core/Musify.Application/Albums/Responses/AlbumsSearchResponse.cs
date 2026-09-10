namespace Musify.Application.Albums.Responses
{
    /// <summary>Result of a combined albums listing/search: a page of local catalog albums
    /// (user-created and previously materialized external albums), plus, when a search term was
    /// given, live external hits appended after them. Pagination metadata (<see cref="PageNumber"/>,
    /// <see cref="PageSize"/>, <see cref="PageCount"/>, <see cref="TotalItemCount"/>) describes only
    /// the local page — the external source has no stable total, so its continuation is tracked
    /// separately via <see cref="NextYoutubeContinuationToken"/>.</summary>
    public record AlbumsSearchResponse(
        IReadOnlyList<AlbumSearchItemResponse> Items,
        int PageNumber,
        int PageSize,
        int PageCount,
        int TotalItemCount,
        bool HasPreviousPage,
        bool HasNextLocalPage,
        string? NextYoutubeContinuationToken,
        bool YoutubeUnavailable,
        bool HasNextPage)
    {
        // HasNextPage is a real field (not a computed property) so it shows up as a required member
        // of the generated OpenAPI schema, same as every other field here — a get-only property added
        // outside the primary constructor gets reflected as optional instead, which is never true here.
        public static AlbumsSearchResponse Create(
            IReadOnlyList<AlbumSearchItemResponse> items,
            int pageNumber,
            int pageSize,
            int pageCount,
            int totalItemCount,
            bool hasPreviousPage,
            bool hasNextLocalPage,
            string? nextYoutubeContinuationToken,
            bool youtubeUnavailable) =>
            new(items, pageNumber, pageSize, pageCount, totalItemCount, hasPreviousPage, hasNextLocalPage,
                nextYoutubeContinuationToken, youtubeUnavailable,
                HasNextPage: hasNextLocalPage || !string.IsNullOrEmpty(nextYoutubeContinuationToken));
    }
}
