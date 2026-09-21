namespace Musify.Application.Tracks.Responses
{

    public record TracksSearchResponse(
        IReadOnlyList<TrackSearchItemResponse> Items,
        int PageNumber,
        int PageSize,
        int PageCount,
        int TotalItemCount,
        bool HasPreviousPage,
        bool HasNextPage);
}
