namespace Musify.Application.Tracks.Responses
{
    /// <summary>A page of the user's catalog tracks (their own uploads).</summary>
    public record TracksSearchResponse(
        IReadOnlyList<TrackSearchItemResponse> Items,
        int PageNumber,
        int PageSize,
        int PageCount,
        int TotalItemCount,
        bool HasPreviousPage,
        bool HasNextPage);
}
