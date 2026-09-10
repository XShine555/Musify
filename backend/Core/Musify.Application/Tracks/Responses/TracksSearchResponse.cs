namespace Musify.Application.Tracks.Responses
{
    /// <summary>Result of a combined tracks listing/search: a page of local catalog tracks
    /// (uploads and previously provisioned YouTube tracks), plus, when a search term was given,
    /// live YouTube Music hits appended after them. Pagination metadata (<see cref="PageNumber"/>,
    /// <see cref="PageSize"/>, <see cref="PageCount"/>, <see cref="TotalItemCount"/>) describes only
    /// the local page — YouTube has no stable total, so its continuation is tracked separately via
    /// <see cref="NextYoutubeContinuationToken"/>.</summary>
    public record TracksSearchResponse(
        IReadOnlyList<TrackSearchItemResponse> Items,
        int PageNumber,
        int PageSize,
        int PageCount,
        int TotalItemCount,
        bool HasPreviousPage,
        bool HasNextLocalPage,
        string? NextYoutubeContinuationToken,
        bool YoutubeUnavailable)
    {
        public bool HasNextPage => HasNextLocalPage || !string.IsNullOrEmpty(NextYoutubeContinuationToken);
    }
}
